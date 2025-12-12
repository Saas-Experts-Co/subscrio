using Subscrio.Core.Application.Constants;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.Services;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Application.Services;

public class FeatureCheckerService
{
    private readonly FeatureValueResolver _resolver;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;

    public FeatureCheckerService(
        ISubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository,
        IFeatureRepository featureRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _featureRepository = featureRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _resolver = new FeatureValueResolver();
    }

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
        T? defaultValue = default)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            return defaultValue ?? default(T);
        }

        // Get plan
        var plan = await _planRepository.FindByIdAsync(subscription.PlanId);
        if (plan == null)
        {
            return defaultValue ?? default(T);
        }

        // Get feature
        var feature = await _featureRepository.FindByKeyAsync(featureKey);
        if (feature == null)
        {
            return defaultValue ?? default(T);
        }

        // Resolve using hierarchy
        var value = _resolver.Resolve(feature, plan, subscription);
        return value != null ? (T)(object)value : (defaultValue ?? default(T));
    }

    /// <summary>
    /// Check if a feature is enabled for a specific subscription
    /// </summary>
    /// <param name="subscriptionKey">The subscription's external key</param>
    /// <param name="featureKey">The feature's external key</param>
    /// <returns>True if feature is enabled (value is 'true')</returns>
    public async Task<bool> IsEnabledForSubscriptionAsync(
        string subscriptionKey,
        string featureKey)
    {
        var value = await GetValueForSubscriptionAsync<string>(subscriptionKey, featureKey);
        return value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Get all feature values for a specific subscription
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllFeaturesForSubscriptionAsync(string subscriptionKey)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Get plan
        var plan = await _planRepository.FindByIdAsync(subscription.PlanId);
        if (plan == null)
        {
            // Plan not found - return empty map instead of throwing
            return new Dictionary<string, string>();
        }

        // Get product to find features
        var product = await _productRepository.FindByKeyAsync(plan.ProductKey);
        if (product == null)
        {
            throw new NotFoundException($"Product with key '{plan.ProductKey}' not found");
        }

        // Product from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!product.Id.HasValue)
        {
            throw new DomainException($"Product '{product.Key}' does not have an ID.");
        }

        // Get all features for the product
        var features = await _featureRepository.FindByProductAsync(product.Id.Value);

        // Resolve features for this specific subscription
        var resolved = new Dictionary<string, string>();

        foreach (var feature in features)
        {
            var value = _resolver.Resolve(feature, plan, subscription);
            resolved.Add(feature.Key, value);
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
        T? defaultValue = default)
    {
        // Find customer
        var customer = await _customerRepository.FindByKeyAsync(customerKey);
        if (customer == null)
        {
            return defaultValue ?? default(T);
        }

        // Get product
        var product = await _productRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            return defaultValue ?? default(T);
        }

        // Get feature
        var feature = await _featureRepository.FindByKeyAsync(featureKey);
        if (feature == null)
        {
            return defaultValue ?? default(T);
        }

        // Entities from repository always have IDs (BIGSERIAL PRIMARY KEY)
        if (!customer.Id.HasValue)
        {
            return defaultValue ?? default(T);
        }

        // Get active subscriptions for this customer
        var subscriptions = await _subscriptionRepository.FindByCustomerIdAsync(
            customer.Id.Value
        );

        // Batch load all plans to avoid N+1 queries
        var planIds = subscriptions.Select(s => s.PlanId).ToList();
        var plans = await _planRepository.FindByIdsAsync(planIds);
        var planMap = new Dictionary<long, Plan>();
        foreach (var p in plans.Where(p => p.Id.HasValue))
        {
            planMap[p.Id!.Value] = p;
        }

        // Filter subscriptions for this product using in-memory map
        var productSubscriptions = subscriptions.Where(subscription =>
        {
            planMap.TryGetValue(subscription.PlanId, out var plan);
            var status = subscription.Status; // This returns a string from the getter
            return plan != null && plan.ProductKey == productKey &&
                   (status == SubscriptionStatus.Active || status == SubscriptionStatus.Trial);
        }).ToList();

        if (productSubscriptions.Count == 0)
        {
            // No active subscriptions for this product, return feature default
            return feature.DefaultValue != null ? (T)(object)feature.DefaultValue : (defaultValue ?? default(T));
        }

        // Resolve using hierarchy
        string? resolvedValue = null;

        foreach (var subscription in productSubscriptions)
        {
            planMap.TryGetValue(subscription.PlanId, out var plan);
            var value = _resolver.Resolve(feature, plan, subscription);

            // Feature from repository always has ID (BIGSERIAL PRIMARY KEY)
            // If this subscription has an override, use it immediately
            if (feature.Id.HasValue && subscription.GetFeatureOverride(feature.Id.Value) != null)
            {
                resolvedValue = value;
                break;
            }

            // Otherwise keep checking
            if (resolvedValue == null)
            {
                resolvedValue = value;
            }
        }

        return resolvedValue != null ? (T)(object)resolvedValue : (defaultValue ?? default(T));
    }

    /// <summary>
    /// Check if a feature is enabled for a customer in a specific product
    /// </summary>
    public async Task<bool> IsEnabledForCustomerAsync(
        string customerKey,
        string productKey,
        string featureKey)
    {
        var value = await GetValueForCustomerAsync<string>(customerKey, productKey, featureKey);
        return value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Get all feature values for a customer in a specific product
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllFeaturesForCustomerAsync(
        string customerKey,
        string productKey)
    {
        var customer = await _customerRepository.FindByKeyAsync(customerKey);
        if (customer == null)
        {
            return new Dictionary<string, string>();
        }

        // Get product
        var product = await _productRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            return new Dictionary<string, string>();
        }

        // Entities from repository always have IDs (BIGSERIAL PRIMARY KEY)
        if (!product.Id.HasValue)
        {
            return new Dictionary<string, string>();
        }

        // Get all features for the product
        var features = await _featureRepository.FindByProductAsync(product.Id.Value);

        // Customer from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!customer.Id.HasValue)
        {
            return new Dictionary<string, string>();
        }

        // Get active subscriptions for this customer
        var subscriptions = await _subscriptionRepository.FindByCustomerIdAsync(
            customer.Id.Value
        );

        // Batch load all plans to avoid N+1 queries
        var planIds = subscriptions.Select(s => s.PlanId).ToList();
        var plans = await _planRepository.FindByIdsAsync(planIds);
        var planMap = new Dictionary<long, Plan>();
        foreach (var p in plans.Where(p => p.Id.HasValue))
        {
            planMap[p.Id!.Value] = p;
        }

        // Filter subscriptions for this product using in-memory map
        var productSubscriptions = subscriptions.Where(subscription =>
        {
            planMap.TryGetValue(subscription.PlanId, out var plan);
            var status = subscription.Status; // This returns a string from the getter
            return plan != null && plan.ProductKey == productKey &&
                   (status == SubscriptionStatus.Active || status == SubscriptionStatus.Trial);
        }).ToList();

        if (productSubscriptions.Count == 0)
        {
            // No active subscriptions for this product, return feature defaults
            var resolved = new Dictionary<string, string>();
            foreach (var feature in features)
            {
                resolved.Add(feature.Key, feature.DefaultValue);
            }
            return resolved;
        }

        // Resolve all features
        return _resolver.ResolveAll(features, planMap, productSubscriptions);
    }

    /// <summary>
    /// Check if customer has access to a specific plan
    /// </summary>
    public async Task<bool> HasPlanAccessAsync(
        string customerKey,
        string productKey,
        string planKey)
    {
        var customer = await _customerRepository.FindByKeyAsync(customerKey);
        if (customer == null)
        {
            return false;
        }

        var product = await _productRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            return false;
        }

        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            return false;
        }

        // Entities from repository always have IDs (BIGSERIAL PRIMARY KEY)
        if (!customer.Id.HasValue || !plan.Id.HasValue)
        {
            return false;
        }

        var subscriptions = await _subscriptionRepository.FindByCustomerIdAsync(
            customer.Id.Value
        );

        return subscriptions.Any(s =>
            s.PlanId == plan.Id.Value &&
            (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
        );
    }

    /// <summary>
    /// Get all active plans for a customer
    /// </summary>
    public async Task<IReadOnlyList<string>> GetActivePlansAsync(string customerKey)
    {
        var customer = await _customerRepository.FindByKeyAsync(customerKey);
        if (customer == null)
        {
            return new List<string>();
        }

        // Customer from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!customer.Id.HasValue)
        {
            return new List<string>();
        }

        var subscriptions = await _subscriptionRepository.FindByCustomerIdAsync(
            customer.Id.Value
        );

        // Batch load all plans to avoid N+1 queries
        var planIds = subscriptions.Select(s => s.PlanId).ToList();
        var plans = await _planRepository.FindByIdsAsync(planIds);

        return plans.Select(plan => plan.Key).ToList();
    }

    /// <summary>
    /// Get feature usage summary for a customer in a specific product
    /// </summary>
    public async Task<FeatureUsageSummaryDto> GetFeatureUsageSummaryAsync(
        string customerKey,
        string productKey)
    {
        var customer = await _customerRepository.FindByKeyAsync(customerKey);
        // Customer from repository always has ID (BIGSERIAL PRIMARY KEY)
        var activeSubscriptions = customer != null && customer.Id.HasValue
            ? (await _subscriptionRepository.FindByCustomerIdAsync(customer.Id.Value)).Count
            : 0;

        var allFeatures = await GetAllFeaturesForCustomerAsync(customerKey, productKey);

        var enabledFeatures = new List<string>();
        var disabledFeatures = new List<string>();
        var numericFeatures = new Dictionary<string, double>();
        var textFeatures = new Dictionary<string, string>();

        // Get all features to determine their types
        var product = await _productRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            return new FeatureUsageSummaryDto
            {
                ActiveSubscriptions = activeSubscriptions,
                EnabledFeatures = enabledFeatures,
                DisabledFeatures = disabledFeatures,
                NumericFeatures = numericFeatures,
                TextFeatures = textFeatures
            };
        }

        // Product from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!product.Id.HasValue)
        {
            return new FeatureUsageSummaryDto
            {
                ActiveSubscriptions = activeSubscriptions,
                EnabledFeatures = enabledFeatures,
                DisabledFeatures = disabledFeatures,
                NumericFeatures = numericFeatures,
                TextFeatures = textFeatures
            };
        }

        var features = await _featureRepository.FindByProductAsync(product.Id.Value);
        var featureTypeMap = new Dictionary<string, FeatureValueType>();
        foreach (var feature in features)
        {
            featureTypeMap[feature.Key] = feature.Props.ValueType;
        }

        foreach (var (featureKey, value) in allFeatures)
        {
            if (!featureTypeMap.TryGetValue(featureKey, out var valueType))
            {
                continue;
            }

            switch (valueType)
            {
                case FeatureValueType.Toggle:
                    if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        enabledFeatures.Add(featureKey);
                    }
                    else
                    {
                        disabledFeatures.Add(featureKey);
                    }
                    break;
                case FeatureValueType.Numeric:
                    if (double.TryParse(value, out var num) && double.IsFinite(num))
                    {
                        numericFeatures[featureKey] = num;
                    }
                    break;
                case FeatureValueType.Text:
                    textFeatures[featureKey] = value;
                    break;
            }
        }

        return new FeatureUsageSummaryDto
        {
            ActiveSubscriptions = activeSubscriptions,
            EnabledFeatures = enabledFeatures,
            DisabledFeatures = disabledFeatures,
            NumericFeatures = numericFeatures,
            TextFeatures = textFeatures
        };
    }
}

