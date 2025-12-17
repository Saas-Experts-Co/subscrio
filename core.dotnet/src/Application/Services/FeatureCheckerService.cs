using Subscrio.Core.Application.Constants;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.Services;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database;

namespace Subscrio.Core.Application.Services;

public class FeatureCheckerService
{
    private readonly FeatureValueResolver _resolver;

    public FeatureCheckerService(
        ISubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository,
        IFeatureRepository featureRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository
    )
    {
        SubscriptionRepository = subscriptionRepository;
        PlanRepository = planRepository;
        FeatureRepository = featureRepository;
        CustomerRepository = customerRepository;
        ProductRepository = productRepository;
        _resolver = new FeatureValueResolver();
    }

    private ISubscriptionRepository SubscriptionRepository { get; }
    private IPlanRepository PlanRepository { get; }
    private IFeatureRepository FeatureRepository { get; }
    private ICustomerRepository CustomerRepository { get; }
    private IProductRepository ProductRepository { get; }

    /// <summary>
    /// Get feature value for a specific subscription
    /// </summary>
    /// <param name="subscriptionKey">The subscription's external key</param>
    /// <param name="featureKey">The feature's external key</param>
    /// <param name="defaultValue">Default value if feature not found</param>
    /// <returns>The resolved feature value or default</returns>
    public async Task<T?> GetValueForSubscriptionAsync<T>(
        string subscriptionKey,
        string featureKey,
        T? defaultValue = default
    )
    {
        var subscriptionView = await SubscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscriptionView == null)
        {
            return defaultValue ?? default;
        }

        // Get plan
        var planRecord = await PlanRepository.FindByIdAsync(subscriptionView.PlanId);
        if (planRecord == null)
        {
            return defaultValue ?? default;
        }

        // Get feature
        var featureRecord = await FeatureRepository.FindByKeyAsync(featureKey);
        if (featureRecord == null)
        {
            return defaultValue ?? default;
        }

        // Convert to domain entities for resolver
        var feature = FeatureMapper.ToDomain(featureRecord);
        // TODO: Load plan feature values and subscription overrides
        var plan = PlanMapper.ToDomain(planRecord, "", null, new List<PlanFeatureValue>());
        var subscription = SubscriptionMapper.ToDomain(subscriptionView, new List<FeatureOverride>());

        // Resolve using hierarchy
        var value = _resolver.Resolve(feature, plan, subscription);
        return value != null ? (T)(object)value : (defaultValue ?? default);
    }

    /// <summary>
    /// Check if a feature is enabled for a specific subscription
    /// </summary>
    /// <param name="subscriptionKey">The subscription's external key</param>
    /// <param name="featureKey">The feature's external key</param>
    /// <returns>True if feature is enabled (value is 'true')</returns>
    public async Task<bool> IsEnabledForSubscriptionAsync(
        string subscriptionKey,
        string featureKey
    )
    {
        var value = await GetValueForSubscriptionAsync<string>(subscriptionKey, featureKey);
        return value?.ToLowerInvariant() == "true";
    }

    /// <summary>
    /// Get all feature values for a specific subscription
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllFeaturesForSubscriptionAsync(
        string subscriptionKey
    )
    {
        var subscriptionView = await SubscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscriptionView == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Get plan
        var planRecord = await PlanRepository.FindByIdAsync(subscriptionView.PlanId);
        if (planRecord == null)
        {
            // Plan not found - return empty dictionary instead of throwing
            return new Dictionary<string, string>();
        }

        // Get product to find features
        var product = await ProductRepository.FindByIdAsync(planRecord.ProductId);
        if (product == null)
        {
            throw new NotFoundException("Product not found for plan");
        }

        // Get all features for the product
        var features = await FeatureRepository.FindByProductAsync(product.Id);

        // Resolve features for this specific subscription
        var resolved = new Dictionary<string, string>();

        // Convert to domain entities
        var plan = PlanMapper.ToDomain(planRecord, product.Key, null, new List<PlanFeatureValue>());
        var subscription = SubscriptionMapper.ToDomain(subscriptionView, new List<FeatureOverride>());

        foreach (var featureRecord in features)
        {
            var feature = FeatureMapper.ToDomain(featureRecord);
            var value = _resolver.Resolve(feature, plan, subscription);
            resolved[feature.Key] = value;
        }

        return resolved;
    }

    /// <summary>
    /// Get feature value for a customer in a specific product
    /// </summary>
    /// <param name="customerKey">The customer's external key</param>
    /// <param name="productKey">The product's external key</param>
    /// <param name="featureKey">The feature's external key</param>
    /// <param name="defaultValue">Default value if feature not found</param>
    /// <returns>The resolved feature value or default</returns>
    public async Task<T?> GetValueForCustomerAsync<T>(
        string customerKey,
        string productKey,
        string featureKey,
        T? defaultValue = default
    )
    {
        // Find customer
        var customer = await CustomerRepository.FindByKeyAsync(customerKey);
        if (customer == null)
        {
            return defaultValue ?? default;
        }

        // Get product
        var product = await ProductRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            return defaultValue ?? default;
        }

        // Get feature
        var feature = await FeatureRepository.FindByKeyAsync(featureKey);
        if (feature == null)
        {
            return defaultValue ?? default;
        }

        // Get active subscriptions for this customer
        var subscriptions = await SubscriptionRepository.FindByCustomerIdAsync(
            customer.Id,
            new SubscriptionFilterDto
            {
                Limit = ApplicationConstants.MaxSubscriptionsPerCustomer,
                Offset = 0
            }
        );

        // Batch load all plans to avoid N+1 queries
        var planIds = subscriptions.Select(s => s.PlanId).Distinct().ToList();
        var plans = await PlanRepository.FindByIdsAsync(planIds);
        var planMap = plans.ToDictionary(p => p.Id, p => p);

        // Get product records for plans to check ProductId
        var productIds = plans.Select(p => p.ProductId).Distinct().ToList();
        var products = await ProductRepository.FindByIdsAsync(productIds);
        var productMap = products.ToDictionary(p => p.Id, p => p);

        // Filter subscriptions for this product using in-memory map
        var productSubscriptions = new List<SubscriptionStatusViewRecord>();
        foreach (var subscriptionView in subscriptions)
        {
            if (!planMap.TryGetValue(subscriptionView.PlanId, out var planRecord))
            {
                continue;
            }

            if (!productMap.TryGetValue(planRecord.ProductId, out var productRecord))
            {
                continue;
            }

            if (productRecord.Key != productKey)
            {
                continue;
            }

            var status = subscriptionView.ComputedStatus.ToLowerInvariant();
            if (status == "active" || status == "trial")
            {
                productSubscriptions.Add(subscriptionView);
            }
        }

        // Convert feature to domain entity once
        var featureDomain = FeatureMapper.ToDomain(feature);
        
        if (productSubscriptions.Count == 0)
        {
            // No active subscriptions for this product, return feature default
            return featureDomain.DefaultValue != null ? (T)(object)featureDomain.DefaultValue : (defaultValue ?? default);
        }

        // Resolve using hierarchy
        string? resolvedValue = null;

        foreach (var subscriptionView in productSubscriptions)
        {
            var planRecord = planMap[subscriptionView.PlanId];
            var productRecord = productMap[planRecord.ProductId];
            
            // TODO: Load plan feature values and subscription overrides
            var plan = PlanMapper.ToDomain(planRecord, productRecord.Key, null, new List<PlanFeatureValue>());
            var subscription = SubscriptionMapper.ToDomain(subscriptionView, new List<FeatureOverride>());
            
            var value = _resolver.Resolve(featureDomain, plan, subscription);

            // If this subscription has an override, use it immediately
            // TODO: Check override properly
            if (resolvedValue == null)
            {
                resolvedValue = value;
            }
        }

        return resolvedValue != null ? (T)(object)resolvedValue : (defaultValue ?? default);
    }

    /// <summary>
    /// Check if a feature is enabled for a customer in a specific product
    /// </summary>
    public async Task<bool> IsEnabledForCustomerAsync(
        string customerKey,
        string productKey,
        string featureKey
    )
    {
        var value = await GetValueForCustomerAsync<string>(customerKey, productKey, featureKey);
        return value?.ToLowerInvariant() == "true";
    }

    /// <summary>
    /// Get all feature values for a customer in a specific product
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllFeaturesForCustomerAsync(
        string customerKey,
        string productKey
    )
    {
        var customer = await CustomerRepository.FindByKeyAsync(customerKey);
        if (customer == null)
        {
            return new Dictionary<string, string>();
        }

        // Get product
        var product = await ProductRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            return new Dictionary<string, string>();
        }

        // Get all features for the product
        var features = await FeatureRepository.FindByProductAsync(product.Id);

        // Get active subscriptions for this customer
        var subscriptions = await SubscriptionRepository.FindByCustomerIdAsync(
            customer.Id,
            new SubscriptionFilterDto
            {
                Limit = ApplicationConstants.MaxSubscriptionsPerCustomer,
                Offset = 0
            }
        );

        // Batch load all plans to avoid N+1 queries
        var planIds = subscriptions.Select(s => s.PlanId).ToList();
        var plans = await PlanRepository.FindByIdsAsync(planIds);
        var planMap = plans.ToDictionary(p => p.Id, p => p);
        
        // Get product records for plans to check ProductId
        var productIds = plans.Select(p => p.ProductId).Distinct().ToList();
        var productRecords = await ProductRepository.FindByIdsAsync(productIds);
        var productMap = productRecords.ToDictionary(p => p.Id, p => p);

        // Filter subscriptions for this product using in-memory map
        var productSubscriptions = subscriptions.Where(subscription =>
        {
            if (!planMap.TryGetValue(subscription.PlanId, out var plan))
            {
                return false;
            }

            // Get product for plan to check ProductKey
            if (!productMap.TryGetValue(plan.ProductId, out var planProduct))
            {
                return false;
            }
            
            var status = subscription.ComputedStatus.ToLowerInvariant();
            return planProduct.Key == productKey &&
                   (status == "active" ||
                    status == "trial");
        }).ToList();

        if (productSubscriptions.Count == 0)
        {
            // No active subscriptions for this product, return feature defaults
            var resolved = new Dictionary<string, string>();
            foreach (var feature in features)
            {
                resolved[feature.Key] = feature.DefaultValue;
            }

            return resolved;
        }

        // Resolve all features - convert to domain entities
        var featureDomains = features.Select(FeatureMapper.ToDomain).ToList();
        var planDomains = new Dictionary<long, Plan>();
        foreach (var kvp in planMap)
        {
            var productRecord = productMap[kvp.Value.ProductId];
            planDomains[kvp.Key] = PlanMapper.ToDomain(kvp.Value, productRecord.Key, null, new List<PlanFeatureValue>());
        }
        var subscriptionDomains = productSubscriptions.Select(sv => SubscriptionMapper.ToDomain(sv, new List<FeatureOverride>())).ToList();
        
        return _resolver.ResolveAll(featureDomains, planDomains, subscriptionDomains);
    }

    /// <summary>
    /// Check if customer has access to a specific plan
    /// </summary>
    public async Task<bool> HasPlanAccessAsync(
        string customerKey,
        string productKey,
        string planKey
    )
    {
        var customer = await CustomerRepository.FindByKeyAsync(customerKey);
        if (customer == null)
        {
            return false;
        }

        var product = await ProductRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            return false;
        }

        var plan = await PlanRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            return false;
        }

        var subscriptions = await SubscriptionRepository.FindByCustomerIdAsync(
            customer.Id,
            new SubscriptionFilterDto
            {
                Limit = 100,
                Offset = 0
            }
        );

        return subscriptions.Any(s =>
            s.PlanId == plan.Id &&
            (s.ComputedStatus.ToLowerInvariant() == "active" ||
             s.ComputedStatus.ToLowerInvariant() == "trial")
        );
    }

    /// <summary>
    /// Get all active plans for a customer
    /// </summary>
    public async Task<List<string>> GetActivePlansAsync(string customerKey)
    {
        var customer = await CustomerRepository.FindByKeyAsync(customerKey);
        if (customer == null)
        {
            return new List<string>();
        }

        var subscriptions = await SubscriptionRepository.FindByCustomerIdAsync(
            customer.Id,
            new SubscriptionFilterDto
            {
                Limit = 100,
                Offset = 0
            }
        );

        // Batch load all plans to avoid N+1 queries
        var planIds = subscriptions.Select(s => s.PlanId).ToList();
        var plans = await PlanRepository.FindByIdsAsync(planIds);

        return plans.Select(plan => plan.Key).ToList();
    }

    /// <summary>
    /// Get feature usage summary for a customer in a specific product
    /// </summary>
    public async Task<FeatureUsageSummaryDto> GetFeatureUsageSummaryAsync(
        string customerKey,
        string productKey
    )
    {
        var customer = await CustomerRepository.FindByKeyAsync(customerKey);
        var activeSubscriptions = customer != null
            ? (await SubscriptionRepository.FindByCustomerIdAsync(
                customer.Id,
                new SubscriptionFilterDto { Limit = 100, Offset = 0 }
            )).Count
            : 0;

        var allFeatures = await GetAllFeaturesForCustomerAsync(customerKey, productKey);

        var enabledFeatures = new List<string>();
        var disabledFeatures = new List<string>();
        var numericFeatures = new Dictionary<string, double>();
        var textFeatures = new Dictionary<string, string>();

        // Get all features to determine their types
        var product = await ProductRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            return new FeatureUsageSummaryDto(
                activeSubscriptions,
                enabledFeatures,
                disabledFeatures,
                numericFeatures,
                textFeatures
            );
        }

        var features = await FeatureRepository.FindByProductAsync(product.Id);
        var featureTypeMap = features.ToDictionary(f => f.Key, f => Enum.Parse<FeatureValueType>(f.ValueType, ignoreCase: true));

        foreach (var (featureKey, value) in allFeatures)
        {
            if (!featureTypeMap.TryGetValue(featureKey, out var valueType))
            {
                continue;
            }

            switch (valueType)
            {
                case FeatureValueType.Toggle:
                    if (value.ToLowerInvariant() == "true")
                    {
                        enabledFeatures.Add(featureKey);
                    }
                    else
                    {
                        disabledFeatures.Add(featureKey);
                    }

                    break;
                case FeatureValueType.Numeric:
                    if (double.TryParse(value, out var num))
                    {
                        numericFeatures[featureKey] = num;
                    }

                    break;
                case FeatureValueType.Text:
                    textFeatures[featureKey] = value;
                    break;
            }
        }

        return new FeatureUsageSummaryDto(
            activeSubscriptions,
            enabledFeatures,
            disabledFeatures,
            numericFeatures,
            textFeatures
        );
    }
}
