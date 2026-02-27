using FluentValidation;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Application.Utils;
using Subscrio.Core.Application.Validators;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database;
using Subscrio.Core.Infrastructure.Utils;
using ValidationException = Subscrio.Core.Application.Errors.ValidationException;

namespace Subscrio.Core.Application.Services;

public class SubscriptionManagementService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IBillingCycleRepository _billingCycleRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IProductRepository _productRepository;
    private readonly CreateSubscriptionDtoValidator _createValidator;
    private readonly UpdateSubscriptionDtoValidator _updateValidator;
    private readonly SubscriptionFilterDtoValidator _filterValidator;
    private readonly DetailedSubscriptionFilterDtoValidator _detailedFilterValidator;

    public SubscriptionManagementService(
        ISubscriptionRepository subscriptionRepository,
        ICustomerRepository customerRepository,
        IPlanRepository planRepository,
        IBillingCycleRepository billingCycleRepository,
        IFeatureRepository featureRepository,
        IProductRepository productRepository,
        CreateSubscriptionDtoValidator createValidator,
        UpdateSubscriptionDtoValidator updateValidator,
        SubscriptionFilterDtoValidator filterValidator,
        DetailedSubscriptionFilterDtoValidator detailedFilterValidator)
    {
        _subscriptionRepository = subscriptionRepository;
        _customerRepository = customerRepository;
        _planRepository = planRepository;
        _billingCycleRepository = billingCycleRepository;
        _featureRepository = featureRepository;
        _productRepository = productRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
        _detailedFilterValidator = detailedFilterValidator;
    }

    private async Task<(string CustomerKey, string ProductKey, string PlanKey, string BillingCycleKey)> ResolveSubscriptionKeysAsync(SubscriptionStatusViewRecord subscription)
    {
        // Get customer
        var customer = await _customerRepository.FindByIdAsync(subscription.CustomerId);
        if (customer == null)
        {
            throw new NotFoundException(
                $"Customer not found for subscription '{subscription.Key}'. " +
                "This indicates data integrity issue - subscription references invalid customer."
            );
        }

        // Get plan
        var plan = await _planRepository.FindByIdAsync(subscription.PlanId);
        if (plan == null)
        {
            throw new NotFoundException(
                $"Plan not found for subscription '{subscription.Key}'. " +
                "This indicates data integrity issue - subscription references invalid plan."
            );
        }

        // Get product to resolve ProductKey
        var product = await _productRepository.FindByIdAsync(plan.ProductId);
        if (product == null)
        {
            throw new NotFoundException("Product not found for plan");
        }

        // Get billing cycle (required)
        var cycle = await _billingCycleRepository.FindByIdAsync(subscription.BillingCycleId);
        if (cycle == null)
        {
            throw new NotFoundException(
                $"Billing cycle not found for subscription '{subscription.Key}'. " +
                "This indicates data integrity issue - subscription references invalid billing cycle."
            );
        }

        return (customer.Key, product.Key, plan.Key, cycle.Key);
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Invalid subscription data",
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
        var plan = await _planRepository.FindByIdAsync(billingCycle.PlanId);
        if (plan == null)
        {
            throw new NotFoundException($"Plan not found for billing cycle '{dto.BillingCycleKey}'");
        }

        var billingCycleId = billingCycle.Id;

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

        var trialEndDate = dto.TrialEndDate;

        // Calculate currentPeriodEnd based on billing cycle duration
        var currentPeriodStart = dto.CurrentPeriodStart ?? DateHelper.Now();
        var billingCycleDomain = BillingCycleMapper.ToDomain(billingCycle);
        var currentPeriodEnd = dto.CurrentPeriodEnd ?? CalculatePeriodEnd(currentPeriodStart, billingCycleDomain);

        // Create record from DTO
        var record = new SubscriptionRecord
        {
            Id = 0, // Will be set by EF Core
            Key = dto.Key,
            CustomerId = customer.Id,
            PlanId = plan.Id,
            BillingCycleId = billingCycleId,
            IsArchived = false,
            ActivationDate = dto.ActivationDate ?? DateHelper.Now(),
            ExpirationDate = dto.ExpirationDate,
            CancellationDate = dto.CancellationDate,
            TrialEndDate = trialEndDate,
            CurrentPeriodStart = currentPeriodStart,
            CurrentPeriodEnd = currentPeriodEnd,
            StripeSubscriptionId = dto.StripeSubscriptionId,
            Metadata = dto.Metadata,
            CreatedAt = DateHelper.Now(),
            UpdatedAt = DateHelper.Now()
        };

        // Save record
        var savedRecord = await _subscriptionRepository.SaveAsync(record);

        // Load from view to get computed status for DTO
        var viewRecord = await _subscriptionRepository.FindByKeyAsync(savedRecord.Key);
        if (viewRecord == null)
        {
            throw new NotFoundException("Failed to load created subscription");
        }

        var keys = await ResolveSubscriptionKeysAsync(viewRecord);
        
        // Load feature overrides for mapper (empty for new subscriptions)
        var overrides = new List<FeatureOverride>();
        
        var subscription = SubscriptionMapper.ToDomain(viewRecord, overrides);
        return SubscriptionMapper.ToDto(
            subscription,
            keys.CustomerKey,
            keys.ProductKey,
            keys.PlanKey,
            keys.BillingCycleKey
        );
    }

    public async Task<SubscriptionDto> UpdateSubscriptionAsync(string subscriptionKey, UpdateSubscriptionDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Invalid update data",
                validationResult.Errors
            );
        }

        // Check if trialEndDate was explicitly set to null/undefined in original input
        var wasTrialEndDateCleared = dto.TrialEndDate == null;

        // Load tracked record for updates
        var record = await _subscriptionRepository.FindByKeyForUpdateAsync(subscriptionKey);
        if (record == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Block updates if subscription is archived
        if (record.IsArchived)
        {
            throw new DomainException(
                $"Cannot update archived subscription with key '{subscriptionKey}'. " +
                "Please unarchive the subscription first."
            );
        }

        // Update properties directly on record (activationDate is immutable)
        if (dto.ExpirationDate != null)
        {
            record.ExpirationDate = dto.ExpirationDate;
        }
        if (dto.CancellationDate != null)
        {
            record.CancellationDate = dto.CancellationDate;
        }
        // Handle trialEndDate updates
        if (dto.TrialEndDate != null || wasTrialEndDateCleared)
        {
            record.TrialEndDate = dto.TrialEndDate;
        }
        if (dto.CurrentPeriodStart != null)
        {
            record.CurrentPeriodStart = dto.CurrentPeriodStart;
        }
        if (dto.CurrentPeriodEnd != null)
        {
            record.CurrentPeriodEnd = dto.CurrentPeriodEnd;
        }
        if (dto.Metadata != null)
        {
            record.Metadata = dto.Metadata;
        }
        if (dto.BillingCycleKey != null)
        {
            // Find the new billing cycle
            var billingCycle = await _billingCycleRepository.FindByKeyAsync(dto.BillingCycleKey);
            if (billingCycle == null)
            {
                throw new NotFoundException($"Billing cycle with key '{dto.BillingCycleKey}' not found");
            }

            record.BillingCycleId = billingCycle.Id;
            record.PlanId = billingCycle.PlanId; // Update plan ID to match new billing cycle
        }

        record.UpdatedAt = DateHelper.Now();

        // Save the same tracked record
        var savedRecord = await _subscriptionRepository.SaveAsync(record);

        // Load from view to get computed status for DTO
        var viewRecord = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (viewRecord == null)
        {
            throw new NotFoundException("Failed to load updated subscription");
        }

        var keys = await ResolveSubscriptionKeysAsync(viewRecord);
        
        // Load feature overrides for mapper
        var overrides = new List<FeatureOverride>(); // TODO: Load actual overrides
        
        var subscription = SubscriptionMapper.ToDomain(viewRecord, overrides);
        return SubscriptionMapper.ToDto(subscription, keys.CustomerKey, keys.ProductKey, keys.PlanKey, keys.BillingCycleKey);
    }

    public async Task<SubscriptionDto?> GetSubscriptionAsync(string subscriptionKey)
    {
        var viewRecord = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (viewRecord == null) return null;

        var keys = await ResolveSubscriptionKeysAsync(viewRecord);
        
        // Load feature overrides for mapper
        var overrides = new List<FeatureOverride>(); // TODO: Load actual overrides
        
        var subscription = SubscriptionMapper.ToDomain(viewRecord, overrides);
        return SubscriptionMapper.ToDto(subscription, keys.CustomerKey, keys.ProductKey, keys.PlanKey, keys.BillingCycleKey);
    }

    /// <summary>
    /// Resolve filter keys to IDs for database querying
    /// Returns null if any required entity is not found (to indicate empty result)
    /// </summary>
    private async Task<Dictionary<string, object?>?> ResolveFilterKeysAsync(SubscriptionFilterDto filters)
    {
        var resolved = new Dictionary<string, object?>();

        // Resolve customerKey to customerId
        if (filters.CustomerKey != null)
        {
            var customer = await _customerRepository.FindByKeyAsync(filters.CustomerKey);
            if (customer == null)
            {
                // Customer not found - return null to indicate empty result
                return null;
            }
            resolved["customerId"] = customer.Id;
        }

        // Resolve planKey and/or productKey to planIds
        if (filters.PlanKey != null)
        {
            if (filters.ProductKey != null)
            {
                // Both planKey and productKey - find specific plan
                var plan = await _planRepository.FindByKeyAsync(filters.PlanKey);
                if (plan == null)
                {
                    return null;
                }
                // Check if plan belongs to product
                var product = await _productRepository.FindByIdAsync(plan.ProductId);
                if (product == null || product.Key != filters.ProductKey)
                {
                    // Plan doesn't belong to product - return null to indicate empty result
                    return null;
                }
                resolved["planId"] = plan.Id;
            }
            else
            {
                // Only planKey - plan keys are globally unique, so findByKey is sufficient
                var plan = await _planRepository.FindByKeyAsync(filters.PlanKey);
                if (plan == null)
                {
                    return null;
                }
                resolved["planId"] = plan.Id;
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
            var planIds = plans.Select(p => p.Id).ToList();
            resolved["planIds"] = planIds;
        }

        return resolved;
    }

    /// <summary>
    /// Resolve filter keys to IDs for database querying (detailed filters)
    /// Returns null if any required entity is not found (to indicate empty result)
    /// </summary>
    private async Task<Dictionary<string, object?>?> ResolveDetailedFilterKeysAsync(DetailedSubscriptionFilterDto filters)
    {
        var resolved = new Dictionary<string, object?>();

        // Resolve customerKey to customerId
        if (filters.CustomerKey != null)
        {
            var customer = await _customerRepository.FindByKeyAsync(filters.CustomerKey);
            if (customer == null)
            {
                return null;
            }
            resolved["customerId"] = customer.Id;
        }

        // Resolve planKey and/or productKey to planIds
        if (filters.PlanKey != null)
        {
            if (filters.ProductKey != null)
            {
                var plan = await _planRepository.FindByKeyAsync(filters.PlanKey);
                if (plan == null)
                {
                    return null;
                }
                // Check if plan belongs to product
                var product = await _productRepository.FindByIdAsync(plan.ProductId);
                if (product == null || product.Key != filters.ProductKey)
                {
                    return null;
                }
                resolved["planId"] = plan.Id;
            }
            else
            {
                var plan = await _planRepository.FindByKeyAsync(filters.PlanKey);
                if (plan == null)
                {
                    return null;
                }
                resolved["planId"] = plan.Id;
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
            var planIds = plans.Select(p => p.Id).ToList();
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
            resolved["billingCycleId"] = billingCycle.Id;
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

        return resolved;
    }

    public async Task<List<SubscriptionDto>> ListSubscriptionsAsync(SubscriptionFilterDto? filters = null)
    {
        var filterDto = filters ?? new SubscriptionFilterDto();
        var validationResult = await _filterValidator.ValidateAsync(filterDto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Invalid filter parameters",
                validationResult.Errors
            );
        }

        // Resolve keys to IDs first
        var resolvedFilters = await ResolveFilterKeysAsync(filterDto);

        // If any key resolution returned null/empty, return empty array
        if (resolvedFilters == null ||
            (resolvedFilters.ContainsKey("planIds") && resolvedFilters["planIds"] is List<long> planIds && planIds.Count == 0))
        {
            return new List<SubscriptionDto>();
        }

        // Merge resolved IDs with other filter properties (sortBy, sortOrder, limit, offset, status, isArchived)
        var dbFilters = new SubscriptionFilterDto(
            CustomerKey: null, // Already resolved to customerId
            ProductKey: null, // Already resolved to planIds
            PlanKey: null, // Already resolved to planId
            Status: filterDto.Status,
            IsArchived: filterDto.IsArchived,
            SortBy: filterDto.SortBy,
            SortOrder: filterDto.SortOrder,
            Limit: filterDto.Limit,
            Offset: filterDto.Offset
        );

        // Query repository with IDs - filtering happens in SQL, returns subscription + customer
        // Note: Repository will need to handle the resolved IDs internally
        // For now, we'll use the filter DTO and let repository handle resolution
        var results = await _subscriptionRepository.FindAllAsync(dbFilters);

        // Map to DTOs
        var dtos = new List<SubscriptionDto>();
        foreach (var result in results)
        {
            // Get keys for plan, product, billing cycle (customer is already available from join)
            var keys = await ResolveSubscriptionKeysAsync(result.Subscription);
            
            // Convert CustomerRecord to domain entity for DTO mapping
            var customerDto = result.Customer != null 
                ? CustomerMapper.ToDto(CustomerMapper.ToDomain(result.Customer)) 
                : null;
            
            // Convert SubscriptionStatusViewRecord to domain entity for DTO mapping
            var featureOverrides = await _subscriptionRepository.GetFeatureOverridesAsync(result.Subscription.Id);
            var overrideList = featureOverrides.Select(fo => new FeatureOverride
            {
                FeatureId = fo.FeatureId,
                Value = fo.Value,
                Type = Enum.Parse<OverrideType>(fo.OverrideType, ignoreCase: true),
                CreatedAt = fo.CreatedAt
            }).ToList();
            var subscription = SubscriptionMapper.ToDomain(result.Subscription, overrideList);
            
            dtos.Add(SubscriptionMapper.ToDto(subscription, keys.CustomerKey, keys.ProductKey, keys.PlanKey, keys.BillingCycleKey, customerDto));
        }
        return dtos;
    }

    public async Task<List<SubscriptionDto>> FindSubscriptionsAsync(DetailedSubscriptionFilterDto filters)
    {
        var validationResult = await _detailedFilterValidator.ValidateAsync(filters);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Invalid filter parameters",
                validationResult.Errors
            );
        }

        // Resolve keys to IDs first
        var resolvedFilters = await ResolveDetailedFilterKeysAsync(filters);

        // If any key resolution returned null/empty, return empty array
        if (resolvedFilters == null ||
            (resolvedFilters.ContainsKey("planIds") && resolvedFilters["planIds"] is List<long> planIds && planIds.Count == 0))
        {
            return new List<SubscriptionDto>();
        }

        // Merge resolved IDs with other filter properties
        var dbFilters = new SubscriptionFilterDto(
            CustomerKey: null,
            ProductKey: null,
            PlanKey: null,
            Status: filters.Status,
            IsArchived: filters.IsArchived,
            SortBy: filters.SortBy,
            SortOrder: filters.SortOrder,
            Limit: filters.Limit,
            Offset: filters.Offset
        );

        // Query repository with IDs
        var results = await _subscriptionRepository.FindAllAsync(dbFilters);

        // Filter by hasFeatureOverrides (unavoidable post-fetch since it requires loading feature overrides)
        var filteredResults = results;
        if (filters.HasFeatureOverrides != null)
        {
            var hasOverrides = filters.HasFeatureOverrides.Value;
            var filteredList = new List<SubscriptionWithCustomerRecord>();
            foreach (var result in results)
            {
                var hasFeatureOverrides = await _subscriptionRepository.HasFeatureOverridesAsync(result.Subscription.Id);
                if (hasOverrides == hasFeatureOverrides)
                {
                    filteredList.Add(result);
                }
            }
            filteredResults = filteredList;
        }

        // Map to DTOs
        var dtos = new List<SubscriptionDto>();
        foreach (var result in filteredResults)
        {
            var keys = await ResolveSubscriptionKeysAsync(result.Subscription);
            
            // Convert CustomerRecord to domain entity for DTO mapping
            var customerDto = result.Customer != null 
                ? CustomerMapper.ToDto(CustomerMapper.ToDomain(result.Customer)) 
                : null;
            
            // Convert SubscriptionStatusViewRecord to domain entity for DTO mapping
            var featureOverrides = await _subscriptionRepository.GetFeatureOverridesAsync(result.Subscription.Id);
            var overrideList = featureOverrides.Select(fo => new FeatureOverride
            {
                FeatureId = fo.FeatureId,
                Value = fo.Value,
                Type = Enum.Parse<OverrideType>(fo.OverrideType, ignoreCase: true),
                CreatedAt = fo.CreatedAt
            }).ToList();
            var subscription = SubscriptionMapper.ToDomain(result.Subscription, overrideList);
            
            dtos.Add(SubscriptionMapper.ToDto(subscription, keys.CustomerKey, keys.ProductKey, keys.PlanKey, keys.BillingCycleKey, customerDto));
        }
        return dtos;
    }

    public async Task<List<SubscriptionDto>> GetSubscriptionsByCustomerAsync(string customerKey)
    {
        var customer = await _customerRepository.FindByKeyAsync(customerKey);
        if (customer == null)
        {
            throw new NotFoundException($"Customer with key '{customerKey}' not found");
        }

        var subscriptions = await _subscriptionRepository.FindByCustomerIdAsync(customer.Id);

        var dtos = new List<SubscriptionDto>();
        foreach (var subscriptionView in subscriptions)
        {
            var keys = await ResolveSubscriptionKeysAsync(subscriptionView);
            var overrides = new List<FeatureOverride>(); // TODO: Load actual overrides
            var subscription = SubscriptionMapper.ToDomain(subscriptionView, overrides);
            dtos.Add(SubscriptionMapper.ToDto(subscription, keys.CustomerKey, keys.ProductKey, keys.PlanKey, keys.BillingCycleKey));
        }
        return dtos;
    }

    public async Task ArchiveSubscriptionAsync(string subscriptionKey)
    {
        // Load tracked record for update
        var record = await _subscriptionRepository.FindByKeyForUpdateAsync(subscriptionKey);
        if (record == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Simple property update - modify record directly
        record.IsArchived = true;
        record.UpdatedAt = DateHelper.Now();
        await _subscriptionRepository.SaveAsync(record);
    }

    public async Task UnarchiveSubscriptionAsync(string subscriptionKey)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Load tracked record for update
        var record = await _subscriptionRepository.FindByKeyForUpdateAsync(subscriptionKey);
        if (record == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Simple property update - modify record directly
        record.IsArchived = false;
        record.UpdatedAt = DateHelper.Now();
        await _subscriptionRepository.SaveAsync(record);
    }

    public async Task DeleteSubscriptionAsync(string subscriptionKey)
    {
        var subscription = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // No deletion constraint - subscriptions can be deleted regardless of status
        await _subscriptionRepository.DeleteAsync(subscription.Id);
    }

    public async Task AddFeatureOverrideAsync(
        string subscriptionKey,
        string featureKey,
        string value,
        OverrideType overrideType = OverrideType.Permanent)
    {
        var subscriptionView = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscriptionView == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Block updates if subscription is archived
        if (subscriptionView.IsArchived)
        {
            throw new DomainException(
                $"Cannot add feature override to archived subscription with key '{subscriptionKey}'. " +
                "Please unarchive the subscription first."
            );
        }

        var featureRecord = await _featureRepository.FindByKeyAsync(featureKey);
        if (featureRecord == null)
        {
            throw new NotFoundException($"Feature with key '{featureKey}' not found");
        }

        // Convert to domain entity for validation
        var feature = FeatureMapper.ToDomain(featureRecord);
        FeatureValueValidator.Validate(value, feature.Props.ValueType);

        // Save feature override directly to database
        await _subscriptionRepository.AddFeatureOverrideAsync(
            subscriptionView.Id,
            featureRecord.Id,
            value,
            overrideType == OverrideType.Permanent ? "permanent" : "temporary"
        );
    }

    public async Task RemoveFeatureOverrideAsync(string subscriptionKey, string featureKey)
    {
        var subscriptionView = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscriptionView == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Block updates if subscription is archived
        if (subscriptionView.IsArchived)
        {
            throw new DomainException(
                $"Cannot remove feature override from archived subscription with key '{subscriptionKey}'. " +
                "Please unarchive the subscription first."
            );
        }

        var featureRecord = await _featureRepository.FindByKeyAsync(featureKey);
        if (featureRecord == null)
        {
            throw new NotFoundException($"Feature with key '{featureKey}' not found");
        }

        // Remove feature override directly from database
        await _subscriptionRepository.RemoveFeatureOverrideAsync(
            subscriptionView.Id,
            featureRecord.Id
        );
    }

    public async Task ClearTemporaryOverridesAsync(string subscriptionKey)
    {
        var subscriptionView = await _subscriptionRepository.FindByKeyAsync(subscriptionKey);
        if (subscriptionView == null)
        {
            throw new NotFoundException($"Subscription with key '{subscriptionKey}' not found");
        }

        // Block updates if subscription is archived
        if (subscriptionView.IsArchived)
        {
            throw new DomainException(
                $"Cannot clear temporary overrides for archived subscription with key '{subscriptionKey}'. " +
                "Please unarchive the subscription first."
            );
        }

        // Clear temporary overrides directly from database
        await _subscriptionRepository.ClearTemporaryOverridesAsync(subscriptionView.Id);
    }

    private DateTime? CalculatePeriodEnd(DateTime startDate, BillingCycle billingCycle)
    {
        return billingCycle.CalculateNextPeriodEnd(startDate);
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
        var versionPattern = new System.Text.RegularExpressions.Regex(@"-v(\d+)$");
        var match = versionPattern.Match(baseKey);

        if (match.Success)
        {
            var currentVersion = int.Parse(match.Groups[1].Value);
            var baseKeyWithoutVersion = versionPattern.Replace(baseKey, "");
            return $"{baseKeyWithoutVersion}-v{currentVersion + 1}";
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
    /// <c>expirationDate &lt;= NOW()</c> and there is no cancellation.
    /// 
    /// </summary>
    /// <returns>Report of processed subscriptions</returns>
    public async Task<TransitionExpiredSubscriptionsReport> TransitionExpiredSubscriptionsAsync()
    {
        var report = new TransitionExpiredSubscriptionsReport(
            Processed: 0,
            Transitioned: 0,
            Archived: 0,
            Errors: new List<TransitionError>()
        );

        // Find all expired subscriptions with transition plans (optimized query with join)
        var expiredSubscriptions = await _subscriptionRepository.FindExpiredWithTransitionPlansAsync(1000);

        foreach (var expiredSubscription in expiredSubscriptions)
        {
            try
            {
                report = report with { Processed = report.Processed + 1 };

                // Get the plan (already verified to have transition in query, but need it for the key)
                var planRecord = await _planRepository.FindByIdAsync(expiredSubscription.PlanId);
                if (planRecord == null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Append(new TransitionError(
                            expiredSubscription.Key,
                            $"Plan with id '{expiredSubscription.PlanId}' not found"
                        )).ToList()
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
                        Errors = report.Errors.Append(new TransitionError(
                            expiredSubscription.Key,
                            $"Customer with id '{expiredSubscription.CustomerId}' not found"
                        )).ToList()
                    };
                    continue;
                }

                // Get transition billing cycle
                if (planRecord.OnExpireTransitionToBillingCycleId == null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Append(new TransitionError(
                            expiredSubscription.Key,
                            $"Plan '{planRecord.Id}' does not have onExpireTransitionToBillingCycleId set"
                        )).ToList()
                    };
                    continue;
                }

                var transitionBillingCycle = await _billingCycleRepository.FindByIdAsync(
                    planRecord.OnExpireTransitionToBillingCycleId.Value
                );
                if (transitionBillingCycle == null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Append(new TransitionError(
                            expiredSubscription.Key,
                            $"Billing cycle with id '{planRecord.OnExpireTransitionToBillingCycleId.Value}' not found"
                        )).ToList()
                    };
                    continue;
                }

                // Mark subscription as transitioned (archives it and sets transitioned_at)
                // Load tracked record for update
                var expiredRecord = await _subscriptionRepository.FindByKeyForUpdateAsync(expiredSubscription.Key);
                if (expiredRecord == null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Append(new TransitionError(
                            expiredSubscription.Key,
                            "Failed to load subscription for update"
                        )).ToList()
                    };
                    continue;
                }
                expiredRecord.IsArchived = true;
                expiredRecord.TransitionedAt = DateHelper.Now();
                expiredRecord.UpdatedAt = DateHelper.Now();
                await _subscriptionRepository.SaveAsync(expiredRecord);
                report = report with { Archived = report.Archived + 1 };

                // Generate versioned key for new subscription
                var newSubscriptionKey = GenerateVersionedKey(expiredSubscription.Key);

                // Check if key already exists (shouldn't happen, but be safe)
                var existing = await _subscriptionRepository.FindByKeyAsync(newSubscriptionKey);
                if (existing != null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Append(new TransitionError(
                            expiredSubscription.Key,
                            $"Generated subscription key '{newSubscriptionKey}' already exists"
                        )).ToList()
                    };
                    continue;
                }

                // Create new subscription to transition billing cycle
                var currentPeriodStart = DateHelper.Now();
                // Convert BillingCycleRecord to domain entity for CalculatePeriodEnd
                var transitionBillingCycleDomain = BillingCycleMapper.ToDomain(transitionBillingCycle);
                var currentPeriodEnd = CalculatePeriodEnd(
                    currentPeriodStart,
                    transitionBillingCycleDomain
                );

                // Get plan for transition billing cycle
                var transitionPlan = await _planRepository.FindByIdAsync(transitionBillingCycle.PlanId);
                if (transitionPlan == null)
                {
                    report = report with
                    {
                        Errors = report.Errors.Append(new TransitionError(
                            expiredSubscription.Key,
                            $"Plan not found for transition billing cycle"
                        )).ToList()
                    };
                    continue;
                }

                // Create new subscription record
                var newSubscription = new SubscriptionRecord
                {
                    Id = 0, // Will be set by EF Core
                    Key = newSubscriptionKey,
                    CustomerId = customer.Id,
                    PlanId = transitionPlan.Id,
                    BillingCycleId = transitionBillingCycle.Id,
                    IsArchived = false,
                    ActivationDate = currentPeriodStart,
                    ExpirationDate = null, // New subscription doesn't expire unless set
                    CancellationDate = null,
                    TrialEndDate = null,
                    CurrentPeriodStart = currentPeriodStart,
                    CurrentPeriodEnd = currentPeriodEnd,
                    StripeSubscriptionId = null, // New subscription doesn't have Stripe ID (old archived subscription keeps its Stripe ID)
                    Metadata = expiredRecord.Metadata, // Carry over metadata
                    CreatedAt = DateHelper.Now(),
                    UpdatedAt = DateHelper.Now()
                };

                await _subscriptionRepository.SaveAsync(newSubscription);
                report = report with { Transitioned = report.Transitioned + 1 };
            }
            catch (Exception error)
            {
                report = report with
                {
                    Errors = report.Errors.Append(new TransitionError(
                        expiredSubscription.Key,
                        error.Message
                    )).ToList()
                };
            }
        }

        return report;
    }
}
