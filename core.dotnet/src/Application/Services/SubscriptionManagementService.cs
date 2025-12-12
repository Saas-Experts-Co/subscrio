using System.Text.RegularExpressions;
using FluentValidation;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Application.Utils;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Utils;

namespace Subscrio.Core.Application.Services;

public class SubscriptionManagementService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IBillingCycleRepository _billingCycleRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IProductRepository _productRepository;
    private readonly IValidator<CreateSubscriptionDto> _createSubscriptionValidator;
    private readonly IValidator<UpdateSubscriptionDto> _updateSubscriptionValidator;
    private readonly IValidator<SubscriptionFilterDto> _subscriptionFilterValidator;
    private readonly IValidator<DetailedSubscriptionFilterDto> _detailedSubscriptionFilterValidator;

    public SubscriptionManagementService(
        ISubscriptionRepository subscriptionRepository,
        ICustomerRepository customerRepository,
        IPlanRepository planRepository,
        IBillingCycleRepository billingCycleRepository,
        IFeatureRepository featureRepository,
        IProductRepository productRepository,
        IValidator<CreateSubscriptionDto> createSubscriptionValidator,
        IValidator<UpdateSubscriptionDto> updateSubscriptionValidator,
        IValidator<SubscriptionFilterDto> subscriptionFilterValidator,
        IValidator<DetailedSubscriptionFilterDto> detailedSubscriptionFilterValidator)
    {
        _subscriptionRepository = subscriptionRepository;
        _customerRepository = customerRepository;
        _planRepository = planRepository;
        _billingCycleRepository = billingCycleRepository;
        _featureRepository = featureRepository;
        _productRepository = productRepository;
        _createSubscriptionValidator = createSubscriptionValidator;
        _updateSubscriptionValidator = updateSubscriptionValidator;
        _subscriptionFilterValidator = subscriptionFilterValidator;
        _detailedSubscriptionFilterValidator = detailedSubscriptionFilterValidator;
    }

    private async Task<(string CustomerKey, string ProductKey, string PlanKey, string BillingCycleKey)> ResolveSubscriptionKeysAsync(Subscription subscription)
    {
        // Get customer
        var customer = await _customerRepository.FindByIdAsync(subscription.CustomerId);
        if (customer == null)
        {
            // This should never happen in normal operation, but log error with subscription key
            throw new NotFoundException(
                $"Customer not found for subscription '{subscription.Key}'. " +
                "This indicates data integrity issue - subscription references invalid customer."
            );
        }

        // Get plan
        var plan = await _planRepository.FindByIdAsync(subscription.PlanId);
        if (plan == null)
        {
            // This should never happen in normal operation, but log error with subscription key
            throw new NotFoundException(
                $"Plan not found for subscription '{subscription.Key}'. " +
                "This indicates data integrity issue - subscription references invalid plan."
            );
        }

        // Get billing cycle (required)
        var cycle = await _billingCycleRepository.FindByIdAsync(subscription.Props.BillingCycleId);
        if (cycle == null)
        {
            // This should never happen in normal operation, but log error with subscription key
            throw new NotFoundException(
                $"Billing cycle not found for subscription '{subscription.Key}'. " +
                "This indicates data integrity issue - subscription references invalid billing cycle."
            );
        }

        return (customer.Key, plan.ProductKey, plan.Key, cycle.Key);
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionDto dto)
    {
        // Validate input
        var validationResult = await _createSubscriptionValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                $"Invalid subscription data: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                validationResult.Errors
            );
        }

        // Verify customer exists
        var customer = await _customerRepository.FindByKeyAsync(dto.CustomerKey);
        if (customer == null)
        {
            throw new NotFoundException($"Customer with key '{dto.CustomerKey}' not found");
        }

        // Get billing cycle and derive plan/product from it
        var billingCycle = await _billingCycleRepository.FindByKeyAsync(dto.BillingCycleKey);
        if (billingCycle == null)
        {
            throw new NotFoundException($"Billing cycle with key '{dto.BillingCycleKey}' not found");
        }

        // Get plan from billing cycle
        var plan = await _planRepository.FindByIdAsync(billingCycle.Props.PlanId);
        if (plan == null)
        {
            throw new NotFoundException($"Plan not found for billing cycle '{dto.BillingCycleKey}'");
        }

        // Get product from plan
        var product = await _productRepository.FindByKeyAsync(plan.ProductKey);
        if (product == null)
        {
            throw new NotFoundException($"Product not found for plan '{plan.Key}'");
        }

        // Billing cycle from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!billingCycle.Id.HasValue)
        {
            throw new DomainException($"Billing cycle '{billingCycle.Key}' does not have an ID.");
        }

        var billingCycleId = billingCycle.Id.Value;

        // Check for duplicate subscription key
        var existingKey = await _subscriptionRepository.FindByKeyAsync(dto.Key);
        if (existingKey != null)
        {
            throw new ConflictException($"Subscription with key '{dto.Key}' already exists");
        }

        // Check for duplicate Stripe subscription ID if provided
        if (dto.StripeSubscriptionId != null)
        {
            var existing = await _subscriptionRepository.FindByStripeIdAsync(dto.StripeSubscriptionId);
            if (existing != null)
            {
                throw new ConflictException($"Subscription with Stripe ID '{dto.StripeSubscriptionId}' already exists");
            }
        }

        // Entities from repository always have IDs (BIGSERIAL PRIMARY KEY)
        // billingCycleId comes from findByKey lookup, so it's guaranteed to have an ID if billingCycle exists
        var trialEndDate = dto.TrialEndDate;

        // Calculate currentPeriodEnd based on billing cycle duration
        var currentPeriodStart = dto.CurrentPeriodStart ?? DateHelper.Now();
        var currentPeriodEnd = dto.CurrentPeriodEnd ?? CalculatePeriodEnd(currentPeriodStart, billingCycle);

        // Create domain entity (no ID - database will generate)
        if (!customer.Id.HasValue || !plan.Id.HasValue)
        {
            throw new DomainException("Customer or Plan does not have an ID.");
        }

        var subscription = new Subscription(new SubscriptionProps(
            dto.Key,  // User-supplied key
            customer.Id.Value,
            plan.Id.Value,
            billingCycleId,
            SubscriptionStatus.Active,  // Default status, will be calculated dynamically
            false,
            dto.ActivationDate ?? DateHelper.Now(),
            dto.ExpirationDate,
            dto.CancellationDate,
            trialEndDate,
            currentPeriodStart,
            currentPeriodEnd,
            dto.StripeSubscriptionId,
            new List<FeatureOverride>(),
            dto.Metadata,
            DateHelper.Now(),
            DateHelper.Now()
        ));

        // Save and get entity with generated ID
        var savedSubscription = await _subscriptionRepository.SaveAsync(subscription);

        var keys = await ResolveSubscriptionKeysAsync(savedSubscription);
        return SubscriptionMapper.ToDto(
            savedSubscription,
            keys.CustomerKey,
            keys.ProductKey,
            keys.PlanKey,
            keys.BillingCycleKey
        );
    }

    public async Task<SubscriptionDto> UpdateSubscriptionAsync(string subscriptionKey, UpdateSubscriptionDto dto)
    {
        // Validate input
        var validationResult = await _updateSubscriptionValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                $"Invalid update data: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                validationResult.Errors
            );
        }

        // Check if trialEndDate was explicitly set to null/undefined in original input
        var wasTrialEndDateCleared = dto.TrialEndDate == null;

        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Block updates if subscription is archived
        if (subscription.IsArchived)
        {
            throw new DomainException(
                $"Cannot update archived subscription with key '{subscriptionKey}'. " +
                "Please unarchive the subscription first."
            );
        }

        // Update properties (activationDate is immutable)
        if (dto.ExpirationDate != null)
        {
            subscription.Props = subscription.Props with { ExpirationDate = dto.ExpirationDate };
        }
        if (dto.CancellationDate != null)
        {
            subscription.Props = subscription.Props with { CancellationDate = dto.CancellationDate };
        }
        // Handle trialEndDate updates
        if (dto.TrialEndDate != null || wasTrialEndDateCleared)
        {
            subscription.Props = subscription.Props with { TrialEndDate = dto.TrialEndDate };
        }
        if (dto.CurrentPeriodStart != null)
        {
            subscription.Props = subscription.Props with { CurrentPeriodStart = dto.CurrentPeriodStart };
        }
        if (dto.CurrentPeriodEnd != null)
        {
            subscription.Props = subscription.Props with { CurrentPeriodEnd = dto.CurrentPeriodEnd };
        }
        if (dto.Metadata != null)
        {
            subscription.Props = subscription.Props with { Metadata = dto.Metadata };
        }
        if (dto.BillingCycleKey != null)
        {
            // Find the new billing cycle
            var billingCycle = await _billingCycleRepository.FindByKeyAsync(dto.BillingCycleKey);
            if (billingCycle == null)
            {
                throw new NotFoundException($"Billing cycle with key '{dto.BillingCycleKey}' not found");
            }

            // Billing cycle from repository always has ID (BIGSERIAL PRIMARY KEY)
            if (!billingCycle.Id.HasValue)
            {
                throw new DomainException($"Billing cycle '{billingCycle.Key}' does not have an ID.");
            }

            subscription.Props = subscription.Props with 
            { 
                BillingCycleId = billingCycle.Id.Value,
                PlanId = billingCycle.Props.PlanId  // Update plan ID to match new billing cycle
            };
        }

        subscription.Props = subscription.Props with { UpdatedAt = DateHelper.Now() };

        var updatedSubscription = await _subscriptionRepository.SaveAsync(subscription);

        var keys = await ResolveSubscriptionKeysAsync(updatedSubscription);
        return SubscriptionMapper.ToDto(updatedSubscription, keys.CustomerKey, keys.ProductKey, keys.PlanKey, keys.BillingCycleKey);
    }

    public async Task<SubscriptionDto?> GetSubscriptionAsync(string subscriptionKey)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null) return null;

        var keys = await ResolveSubscriptionKeysAsync(subscription);
        return SubscriptionMapper.ToDto(subscription, keys.CustomerKey, keys.ProductKey, keys.PlanKey, keys.BillingCycleKey);
    }

    /// <summary>
    /// Resolve filter keys to IDs for database querying
    /// Returns null if any required entity is not found (to indicate empty result)
    /// </summary>
    private async Task<Dictionary<string, object>?> ResolveFilterKeysAsync(SubscriptionFilterDto filters)
    {
        var resolved = new Dictionary<string, object>();

        // Resolve customerKey to customerId
        if (filters.CustomerKey != null)
        {
            var customer = await _customerRepository.FindByKeyAsync(filters.CustomerKey);
            if (customer == null)
            {
                // Customer not found - return null to indicate empty result
                return null;
            }
            // Customer from repository always has ID (BIGSERIAL PRIMARY KEY)
            if (!customer.Id.HasValue)
            {
                return null;
            }
            resolved["customerId"] = customer.Id.Value;
        }

        // Resolve planKey and/or productKey to planIds
        if (filters.PlanKey != null)
        {
            if (filters.ProductKey != null)
            {
                // Both planKey and productKey - find specific plan
                var plan = await _planRepository.FindByKeyAsync(filters.PlanKey);
                if (plan == null || plan.ProductKey != filters.ProductKey)
                {
                    // Plan not found or doesn't belong to product - return null to indicate empty result
                    return null;
                }
                // Plan from repository always has ID (BIGSERIAL PRIMARY KEY)
                if (!plan.Id.HasValue)
                {
                    return null;
                }
                resolved["planId"] = plan.Id.Value;
            }
            else
            {
                // Only planKey - plan keys are globally unique, so FindByKeyAsync is sufficient
                var plan = await _planRepository.FindByKeyAsync(filters.PlanKey);
                if (plan == null)
                {
                    return null;
                }
                // Plan from repository always has ID (BIGSERIAL PRIMARY KEY)
                if (!plan.Id.HasValue)
                {
                    return null;
                }
                resolved["planId"] = plan.Id.Value;
            }
        }
        else if (filters.ProductKey != null)
        {
            // Only productKey - find all plans for this product
            var product = await _productRepository.FindByKeyAsync(filters.ProductKey);
            if (product == null)
            {
                resolved["planIds"] = new List<long>();
                return resolved;
            }
            var plans = await _planRepository.FindByProductAsync(product.Key);
            if (plans.Count == 0)
            {
                resolved["planIds"] = new List<long>();
                return resolved;
            }
            // Plans from repository always have IDs (BIGSERIAL PRIMARY KEY)
            var planIds = plans.Where(p => p.Id.HasValue).Select(p => p.Id!.Value).ToList();
            resolved["planIds"] = planIds;
        }

        // Copy other filter properties
        if (filters.Status != null)
        {
            resolved["status"] = filters.Status;
        }
        if (filters.IsArchived != null)
        {
            resolved["isArchived"] = filters.IsArchived.Value;
        }
        if (filters.SortBy != null)
        {
            resolved["sortBy"] = filters.SortBy;
        }
        if (filters.SortOrder != null)
        {
            resolved["sortOrder"] = filters.SortOrder;
        }
        resolved["limit"] = filters.Limit;
        resolved["offset"] = filters.Offset;

        return resolved;
    }

    /// <summary>
    /// Resolve filter keys to IDs for database querying (DetailedSubscriptionFilterDto version)
    /// Returns null if any required entity is not found (to indicate empty result)
    /// </summary>
    private async Task<Dictionary<string, object>?> ResolveFilterKeysAsync(DetailedSubscriptionFilterDto filters)
    {
        var resolved = new Dictionary<string, object>();

        // Resolve customerKey to customerId
        if (filters.CustomerKey != null)
        {
            var customer = await _customerRepository.FindByKeyAsync(filters.CustomerKey);
            if (customer == null)
            {
                return null;
            }
            if (!customer.Id.HasValue)
            {
                return null;
            }
            resolved["customerId"] = customer.Id.Value;
        }

        // Resolve planKey and/or productKey to planIds
        if (filters.PlanKey != null)
        {
            if (filters.ProductKey != null)
            {
                var plan = await _planRepository.FindByKeyAsync(filters.PlanKey);
                if (plan == null || plan.ProductKey != filters.ProductKey)
                {
                    return null;
                }
                if (!plan.Id.HasValue)
                {
                    return null;
                }
                resolved["planId"] = plan.Id.Value;
            }
            else
            {
                var plan = await _planRepository.FindByKeyAsync(filters.PlanKey);
                if (plan == null)
                {
                    return null;
                }
                if (!plan.Id.HasValue)
                {
                    return null;
                }
                resolved["planId"] = plan.Id.Value;
            }
        }
        else if (filters.ProductKey != null)
        {
            var product = await _productRepository.FindByKeyAsync(filters.ProductKey);
            if (product == null)
            {
                resolved["planIds"] = new List<long>();
                return resolved;
            }
            var plans = await _planRepository.FindByProductAsync(product.Key);
            if (plans.Count == 0)
            {
                resolved["planIds"] = new List<long>();
                return resolved;
            }
            var planIds = plans.Where(p => p.Id.HasValue).Select(p => p.Id!.Value).ToList();
            resolved["planIds"] = planIds;
        }

        // Resolve billingCycleKey to billingCycleId
        if (filters.BillingCycleKey != null)
        {
            var billingCycle = await _billingCycleRepository.FindByKeyAsync(filters.BillingCycleKey);
            if (billingCycle == null)
            {
                return null;
            }
            if (!billingCycle.Id.HasValue)
            {
                return null;
            }
            resolved["billingCycleId"] = billingCycle.Id.Value;
        }

        // Copy other filter properties (date ranges, etc.)
        if (filters.ActivationDateFrom != null)
        {
            resolved["activationDateFrom"] = filters.ActivationDateFrom.Value;
        }
        if (filters.ActivationDateTo != null)
        {
            resolved["activationDateTo"] = filters.ActivationDateTo.Value;
        }
        if (filters.ExpirationDateFrom != null)
        {
            resolved["expirationDateFrom"] = filters.ExpirationDateFrom.Value;
        }
        if (filters.ExpirationDateTo != null)
        {
            resolved["expirationDateTo"] = filters.ExpirationDateTo.Value;
        }
        if (filters.TrialEndDateFrom != null)
        {
            resolved["trialEndDateFrom"] = filters.TrialEndDateFrom.Value;
        }
        if (filters.TrialEndDateTo != null)
        {
            resolved["trialEndDateTo"] = filters.TrialEndDateTo.Value;
        }
        if (filters.CurrentPeriodStartFrom != null)
        {
            resolved["currentPeriodStartFrom"] = filters.CurrentPeriodStartFrom.Value;
        }
        if (filters.CurrentPeriodStartTo != null)
        {
            resolved["currentPeriodStartTo"] = filters.CurrentPeriodStartTo.Value;
        }
        if (filters.CurrentPeriodEndFrom != null)
        {
            resolved["currentPeriodEndFrom"] = filters.CurrentPeriodEndFrom.Value;
        }
        if (filters.CurrentPeriodEndTo != null)
        {
            resolved["currentPeriodEndTo"] = filters.CurrentPeriodEndTo.Value;
        }
        if (filters.HasStripeId != null)
        {
            resolved["hasStripeId"] = filters.HasStripeId.Value;
        }
        if (filters.HasTrial != null)
        {
            resolved["hasTrial"] = filters.HasTrial.Value;
        }

        // Pass through isArchived filter
        if (filters.IsArchived != null)
        {
            resolved["isArchived"] = filters.IsArchived.Value;
        }

        if (filters.Status != null)
        {
            resolved["status"] = filters.Status;
        }
        if (filters.SortBy != null)
        {
            resolved["sortBy"] = filters.SortBy;
        }
        if (filters.SortOrder != null)
        {
            resolved["sortOrder"] = filters.SortOrder;
        }
        resolved["limit"] = filters.Limit;
        resolved["offset"] = filters.Offset;

        return resolved;
    }

    public async Task<IReadOnlyList<SubscriptionDto>> ListSubscriptionsAsync(SubscriptionFilterDto? filters = null)
    {
        filters ??= new SubscriptionFilterDto();
        var validationResult = await _subscriptionFilterValidator.ValidateAsync(filters);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                $"Invalid filter parameters: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                validationResult.Errors
            );
        }

        // Resolve keys to IDs first
        var resolvedFilters = await ResolveFilterKeysAsync(validationResult.Data);

        // If any key resolution returned null/empty, return empty array
        if (resolvedFilters == null ||
            (resolvedFilters.ContainsKey("planIds") && resolvedFilters["planIds"] is List<long> planIds && planIds.Count == 0))
        {
            return new List<SubscriptionDto>();
        }

        // Query repository with IDs - filtering happens in SQL, returns subscription + customer
        var results = await _subscriptionRepository.FindAllAsync(resolvedFilters);

        // Map to DTOs
        var dtos = new List<SubscriptionDto>();
        foreach (var result in results)
        {
            // Get keys for plan, product, billing cycle (customer is already available from join)
            var keys = await ResolveSubscriptionKeysAsync(result.Subscription);
            dtos.Add(SubscriptionMapper.ToDto(result.Subscription, keys.CustomerKey, keys.ProductKey, keys.PlanKey, keys.BillingCycleKey, result.Customer));
        }
        return dtos;
    }

    public async Task<IReadOnlyList<SubscriptionDto>> FindSubscriptionsAsync(DetailedSubscriptionFilterDto filters)
    {
        var validationResult = await _detailedSubscriptionFilterValidator.ValidateAsync(filters);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                $"Invalid filter parameters: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                validationResult.Errors
            );
        }

        // Resolve keys to IDs first
        var resolvedFilters = await ResolveFilterKeysAsync(validationResult.Data);

        // If any key resolution returned null/empty, return empty array
        if (resolvedFilters == null ||
            (resolvedFilters.ContainsKey("planIds") && resolvedFilters["planIds"] is List<long> planIds && planIds.Count == 0))
        {
            return new List<SubscriptionDto>();
        }

        // Query repository with IDs - filtering happens in SQL, returns subscription + customer
        var results = await _subscriptionRepository.FindAllAsync(resolvedFilters);

        // Filter by hasFeatureOverrides (unavoidable post-fetch since it requires loading feature overrides)
        var filteredResults = results;
        if (filters.HasFeatureOverrides != null)
        {
            var hasOverrides = filters.HasFeatureOverrides.Value;
            filteredResults = filteredResults.Where(r =>
                hasOverrides ? r.Subscription.Props.FeatureOverrides.Count > 0 : r.Subscription.Props.FeatureOverrides.Count == 0
            ).ToList();
        }

        // Map to DTOs
        var dtos = new List<SubscriptionDto>();
        foreach (var result in filteredResults)
        {
            // Get keys for plan, product, billing cycle (customer is already available from join)
            var keys = await ResolveSubscriptionKeysAsync(result.Subscription);
            dtos.Add(SubscriptionMapper.ToDto(result.Subscription, keys.CustomerKey, keys.ProductKey, keys.PlanKey, keys.BillingCycleKey, result.Customer));
        }
        return dtos;
    }

    public async Task<IReadOnlyList<SubscriptionDto>> GetSubscriptionsByCustomerAsync(string customerKey)
    {
        var customer = await _customerRepository.FindByKeyAsync(customerKey);
        if (customer == null)
        {
            throw new NotFoundException($"Customer with key '{customerKey}' not found");
        }

        // Customer from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!customer.Id.HasValue)
        {
            throw new DomainException($"Customer '{customer.Key}' does not have an ID.");
        }

        var subscriptions = await _subscriptionRepository.FindByCustomerIdAsync(customer.Id.Value);

        var dtos = new List<SubscriptionDto>();
        foreach (var subscription in subscriptions)
        {
            var keys = await ResolveSubscriptionKeysAsync(subscription);
            dtos.Add(SubscriptionMapper.ToDto(subscription, keys.CustomerKey, keys.ProductKey, keys.PlanKey, keys.BillingCycleKey));
        }
        return dtos;
    }

    public async Task ArchiveSubscriptionAsync(string subscriptionKey)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Archive does not change any properties - just sets the archive flag
        subscription.Archive();
        await _subscriptionRepository.SaveAsync(subscription);
    }

    public async Task UnarchiveSubscriptionAsync(string subscriptionKey)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Unarchive just clears the archive flag
        subscription.Unarchive();
        await _subscriptionRepository.SaveAsync(subscription);
    }

    public async Task DeleteSubscriptionAsync(string subscriptionKey)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Subscription from repository always has ID (BIGSERIAL PRIMARY KEY)
        // No deletion constraint - subscriptions can be deleted regardless of status
        if (!subscription.Id.HasValue)
        {
            throw new DomainException($"Subscription '{subscription.Key}' does not have an ID and cannot be deleted.");
        }

        await _subscriptionRepository.DeleteAsync(subscription.Id.Value);
    }

    public async Task AddFeatureOverrideAsync(
        string subscriptionKey,
        string featureKey,
        string value,
        OverrideType overrideType = OverrideType.Permanent)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Block updates if subscription is archived
        if (subscription.IsArchived)
        {
            throw new DomainException(
                $"Cannot add feature override to archived subscription with key '{subscriptionKey}'. " +
                "Please unarchive the subscription first."
            );
        }

        var feature = await _featureRepository.FindByKeyAsync(featureKey);
        if (feature == null)
        {
            throw new NotFoundException($"Feature with key '{featureKey}' not found");
        }

        // Validate value against feature type
        FeatureValueValidator.Validate(value, feature.Props.ValueType);

        // Feature from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!feature.Id.HasValue)
        {
            throw new DomainException($"Feature '{feature.Key}' does not have an ID.");
        }

        subscription.AddFeatureOverride(feature.Id.Value, value, overrideType);
        await _subscriptionRepository.SaveAsync(subscription);
    }

    public async Task RemoveFeatureOverrideAsync(string subscriptionKey, string featureKey)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Block updates if subscription is archived
        if (subscription.IsArchived)
        {
            throw new DomainException(
                $"Cannot remove feature override from archived subscription with key '{subscriptionKey}'. " +
                "Please unarchive the subscription first."
            );
        }

        var feature = await _featureRepository.FindByKeyAsync(featureKey);
        if (feature == null)
        {
            throw new NotFoundException($"Feature with key '{featureKey}' not found");
        }

        // Feature from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!feature.Id.HasValue)
        {
            throw new DomainException($"Feature '{feature.Key}' does not have an ID.");
        }

        subscription.RemoveFeatureOverride(feature.Id.Value);
        await _subscriptionRepository.SaveAsync(subscription);
    }

    public async Task ClearTemporaryOverridesAsync(string subscriptionKey)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Block updates if subscription is archived
        if (subscription.IsArchived)
        {
            throw new DomainException(
                $"Cannot clear temporary overrides for archived subscription with key '{subscriptionKey}'. " +
                "Please unarchive the subscription first."
            );
        }

        subscription.ClearTemporaryOverrides();
        await _subscriptionRepository.SaveAsync(subscription);
    }

    private DateTime? CalculatePeriodEnd(DateTime startDate, BillingCycle billingCycle)
    {
        // For forever billing cycles, return null (never expires)
        if (billingCycle.Props.DurationUnit == DurationUnit.Forever)
        {
            return null;
        }

        var endDate = startDate;

        switch (billingCycle.Props.DurationUnit)
        {
            case DurationUnit.Days:
                endDate = endDate.AddDays(billingCycle.Props.DurationValue ?? 0);
                break;
            case DurationUnit.Weeks:
                endDate = endDate.AddDays((billingCycle.Props.DurationValue ?? 0) * 7);
                break;
            case DurationUnit.Months:
                endDate = endDate.AddMonths(billingCycle.Props.DurationValue ?? 0);
                break;
            case DurationUnit.Years:
                endDate = endDate.AddYears(billingCycle.Props.DurationValue ?? 0);
                break;
            default:
                throw new ValidationException($"Unknown duration unit: {billingCycle.Props.DurationUnit}");
        }

        return endDate;
    }

    /// <summary>
    /// Generate versioned subscription key from base key
    /// Examples:
    /// - "sub-abc" -> "sub-abc-v1"
    /// - "sub-abc-v1" -> "sub-abc-v2"
    /// - "sub-abc-v5" -> "sub-abc-v6"
    /// </summary>
    private string GenerateVersionedKey(string baseKey)
    {
        var versionPattern = new Regex(@"-v(\d+)$");
        var match = versionPattern.Match(baseKey);

        if (match.Success)
        {
            var currentVersion = int.Parse(match.Groups[1].Value);
            var base = versionPattern.Replace(baseKey, "");
            return $"{base}-v{currentVersion + 1}";
        }
        else
        {
            return $"{baseKey}-v1";
        }
    }

    /// <summary>
    /// Process expired subscriptions and transition them to configured plans.
    /// 
    /// This method:
    /// 1. Finds all expired subscriptions (status='expired', not archived) whose plan has a transition requirement
    /// 2. For each expired subscription:
    ///    - Archives the old subscription
    ///    - Creates a new subscription to the transition billing cycle
    ///    - New subscription key is versioned: original key + "-vX" (or increments if already versioned)
    /// 
    /// Note: Plans do not have grace periods. A subscription is expired when
    /// `expirationDate <= NOW()` and there is no cancellation.
    /// 
    /// </summary>
    /// <returns>Report of processed subscriptions</returns>
    public async Task<TransitionExpiredSubscriptionsReport> TransitionExpiredSubscriptionsAsync()
    {
        var report = new TransitionExpiredSubscriptionsReport
        {
            Processed = 0,
            Transitioned = 0,
            Archived = 0,
            Errors = new List<TransitionError>()
        };

        // Find all expired subscriptions with transition plans (optimized query with join)
        var expiredSubscriptions = await _subscriptionRepository.FindExpiredWithTransitionPlansAsync(1000);

        foreach (var expiredSubscription in expiredSubscriptions)
        {
            try
            {
                report = report with { Processed = report.Processed + 1 };

                // Get the plan (already verified to have transition in query, but need it for the key)
                var plan = await _planRepository.FindByIdAsync(expiredSubscription.PlanId);
                if (plan == null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Concat(new[]
                        {
                            new TransitionError
                            {
                                SubscriptionKey = expiredSubscription.Key,
                                Error = $"Plan with id '{expiredSubscription.PlanId}' not found"
                            }
                        }).ToList()
                    };
                    continue;
                }

                // Plan already verified to have transition requirement in query
                // Transition configured - archive old subscription and create new one
                // Get customer
                var customer = await _customerRepository.FindByIdAsync(expiredSubscription.CustomerId);
                if (customer == null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Concat(new[]
                        {
                            new TransitionError
                            {
                                SubscriptionKey = expiredSubscription.Key,
                                Error = $"Customer with id '{expiredSubscription.CustomerId}' not found"
                            }
                        }).ToList()
                    };
                    continue;
                }

                // Get transition billing cycle
                if (plan.Props.OnExpireTransitionToBillingCycleKey == null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Concat(new[]
                        {
                            new TransitionError
                            {
                                SubscriptionKey = expiredSubscription.Key,
                                Error = $"Plan '{plan.Id}' does not have onExpireTransitionToBillingCycleKey set"
                            }
                        }).ToList()
                    };
                    continue;
                }

                var transitionBillingCycle = await _billingCycleRepository.FindByKeyAsync(
                    plan.Props.OnExpireTransitionToBillingCycleKey
                );
                if (transitionBillingCycle == null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Concat(new[]
                        {
                            new TransitionError
                            {
                                SubscriptionKey = expiredSubscription.Key,
                                Error = $"Billing cycle with key '{plan.Props.OnExpireTransitionToBillingCycleKey}' not found"
                            }
                        }).ToList()
                    };
                    continue;
                }

                // Mark subscription as transitioned (archives it and sets transitioned_at)
                expiredSubscription.MarkAsTransitioned();
                await _subscriptionRepository.SaveAsync(expiredSubscription);
                report = report with { Archived = report.Archived + 1 };

                // Generate versioned key for new subscription
                var newSubscriptionKey = GenerateVersionedKey(expiredSubscription.Key);

                // Check if key already exists (shouldn't happen, but be safe)
                var existing = await _subscriptionRepository.FindByKeyAsync(newSubscriptionKey);
                if (existing != null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Concat(new[]
                        {
                            new TransitionError
                            {
                                SubscriptionKey = expiredSubscription.Key,
                                Error = $"Generated subscription key '{newSubscriptionKey}' already exists"
                            }
                        }).ToList()
                    };
                    continue;
                }

                // Create new subscription to transition billing cycle
                var currentPeriodStart = DateHelper.Now();
                var currentPeriodEnd = CalculatePeriodEnd(
                    currentPeriodStart,
                    transitionBillingCycle
                );

                if (!customer.Id.HasValue || !transitionBillingCycle.Id.HasValue)
                {
                    report = report with
                    {
                        Errors = report.Errors.Concat(new[]
                        {
                            new TransitionError
                            {
                                SubscriptionKey = expiredSubscription.Key,
                                Error = "Customer or BillingCycle does not have an ID"
                            }
                        }).ToList()
                    };
                    continue;
                }

                var newSubscription = new Subscription(new SubscriptionProps(
                    newSubscriptionKey,
                    customer.Id.Value,
                    transitionBillingCycle.Props.PlanId,
                    transitionBillingCycle.Id.Value,
                    SubscriptionStatus.Active,
                    false,
                    currentPeriodStart,
                    null, // New subscription doesn't expire unless set
                    null,
                    null,
                    currentPeriodStart,
                    currentPeriodEnd,
                    null, // New subscription doesn't have Stripe ID (old archived subscription keeps its Stripe ID)
                    new List<FeatureOverride>(), // Overrides don't carry over to new subscription
                    expiredSubscription.Props.Metadata, // Carry over metadata
                    DateHelper.Now(),
                    DateHelper.Now()
                ));

                await _subscriptionRepository.SaveAsync(newSubscription);
                report = report with { Transitioned = report.Transitioned + 1 };
            }
            catch (Exception error)
            {
                report = report with
                {
                    Errors = report.Errors.Concat(new[]
                    {
                        new TransitionError
                        {
                            SubscriptionKey = expiredSubscription.Key,
                            Error = error.Message
                        }
                    }).ToList()
                };
            }
        }

        return report;
    }
}

