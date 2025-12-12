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

public class PlanManagementService
{
    private readonly IPlanRepository _planRepository;
    private readonly IProductRepository _productRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IValidator<CreatePlanDto> _createPlanValidator;
    private readonly IValidator<UpdatePlanDto> _updatePlanValidator;
    private readonly IValidator<PlanFilterDto> _planFilterValidator;

    public PlanManagementService(
        IPlanRepository planRepository,
        IProductRepository productRepository,
        IFeatureRepository featureRepository,
        ISubscriptionRepository subscriptionRepository,
        IValidator<CreatePlanDto> createPlanValidator,
        IValidator<UpdatePlanDto> updatePlanValidator,
        IValidator<PlanFilterDto> planFilterValidator)
    {
        _planRepository = planRepository;
        _productRepository = productRepository;
        _featureRepository = featureRepository;
        _subscriptionRepository = subscriptionRepository;
        _createPlanValidator = createPlanValidator;
        _updatePlanValidator = updatePlanValidator;
        _planFilterValidator = planFilterValidator;
    }

    private async Task<(string ProductKey, string? OnExpireTransitionToBillingCycleKey)> ResolvePlanKeysAsync(Plan plan)
    {
        // Plan now stores productKey directly
        var productKey = plan.ProductKey;

        var onExpireTransitionToBillingCycleKey = plan.Props.OnExpireTransitionToBillingCycleKey;

        return (productKey, onExpireTransitionToBillingCycleKey);
    }

    public async Task<PlanDto> CreatePlanAsync(CreatePlanDto dto)
    {
        // Validate input
        var validationResult = await _createPlanValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                $"Invalid plan data for key '{dto.Key}': {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                validationResult.Errors
            );
        }

        // Verify product exists by key
        var product = await _productRepository.FindByKeyAsync(dto.ProductKey);
        if (product == null)
        {
            throw new NotFoundException($"Product with key '{dto.ProductKey}' not found");
        }

        // Check if plan key already exists globally
        var existing = await _planRepository.FindByKeyAsync(dto.Key);
        if (existing != null)
        {
            throw new ConflictException($"Plan with key '{dto.Key}' already exists");
        }

        // Create domain entity (no ID - database will generate)
        var plan = new Plan(new PlanProps(
            product.Key,
            dto.Key,
            dto.DisplayName,
            dto.Description,
            PlanStatus.Active,
            dto.OnExpireTransitionToBillingCycleKey,
            new List<PlanFeatureValue>(),
            dto.Metadata,
            DateHelper.Now(),
            DateHelper.Now()
        ));

        // Save and get entity with generated ID
        var savedPlan = await _planRepository.SaveAsync(plan);

        var keys = await ResolvePlanKeysAsync(savedPlan);
        return PlanMapper.ToDto(savedPlan, keys.ProductKey, keys.OnExpireTransitionToBillingCycleKey);
    }

    public async Task<PlanDto> UpdatePlanAsync(string planKey, UpdatePlanDto dto)
    {
        // Validate input
        var validationResult = await _updatePlanValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Invalid update data", validationResult.Errors);
        }

        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            throw new NotFoundException($"Plan with key '{planKey}' not found");
        }

        // Update properties
        if (dto.DisplayName != null)
        {
            plan.UpdateDisplayName(dto.DisplayName);
        }
        if (dto.Description != null)
        {
            plan.Props = plan.Props with { Description = dto.Description };
        }
        if (dto.OnExpireTransitionToBillingCycleKey != null)
        {
            plan.Props = plan.Props with { OnExpireTransitionToBillingCycleKey = dto.OnExpireTransitionToBillingCycleKey };
        }
        if (dto.Metadata != null)
        {
            plan.Props = plan.Props with { Metadata = dto.Metadata };
        }

        plan.Props = plan.Props with { UpdatedAt = DateHelper.Now() };
        await _planRepository.SaveAsync(plan);

        var keys = await ResolvePlanKeysAsync(plan);
        return PlanMapper.ToDto(plan, keys.ProductKey, keys.OnExpireTransitionToBillingCycleKey);
    }

    public async Task<PlanDto?> GetPlanAsync(string planKey)
    {
        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            return null;
        }

        var keys = await ResolvePlanKeysAsync(plan);
        return PlanMapper.ToDto(plan, keys.ProductKey, keys.OnExpireTransitionToBillingCycleKey);
    }

    public async Task<IReadOnlyList<PlanDto>> ListPlansAsync(PlanFilterDto? filters = null)
    {
        filters ??= new PlanFilterDto();
        var validationResult = await _planFilterValidator.ValidateAsync(filters);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Invalid filter parameters", validationResult.Errors);
        }

        // Filters are already validated and use productKey
        var resolvedFilters = validationResult.Data;

        var plans = await _planRepository.FindAllAsync(resolvedFilters);

        // Map each plan with resolved keys
        var planDtos = new List<PlanDto>();
        foreach (var plan in plans)
        {
            var keys = await ResolvePlanKeysAsync(plan);
            planDtos.Add(PlanMapper.ToDto(plan, keys.ProductKey, keys.OnExpireTransitionToBillingCycleKey));
        }
        return planDtos;
    }

    public async Task<IReadOnlyList<PlanDto>> GetPlansByProductAsync(string productKey)
    {
        // Verify product exists
        var product = await _productRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            throw new NotFoundException($"Product with key '{productKey}' not found");
        }

        var plans = await _planRepository.FindByProductAsync(product.Key);

        // Map each plan with resolved keys
        var planDtos = new List<PlanDto>();
        foreach (var plan in plans)
        {
            var keys = await ResolvePlanKeysAsync(plan);
            planDtos.Add(PlanMapper.ToDto(plan, keys.ProductKey, keys.OnExpireTransitionToBillingCycleKey));
        }
        return planDtos;
    }

    public async Task ArchivePlanAsync(string planKey)
    {
        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            throw new NotFoundException($"Plan with key '{planKey}' not found");
        }

        plan.Archive();
        await _planRepository.SaveAsync(plan);
    }

    public async Task UnarchivePlanAsync(string planKey)
    {
        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            throw new NotFoundException($"Plan with key '{planKey}' not found");
        }

        plan.Unarchive();
        await _planRepository.SaveAsync(plan);
    }

    public async Task DeletePlanAsync(string planKey)
    {
        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            throw new NotFoundException($"Plan with key '{planKey}' not found");
        }

        if (!plan.CanDelete())
        {
            throw new DomainException(
                $"Cannot delete plan with status '{plan.Status}'. " +
                "Plan must be archived before deletion."
            );
        }

        // Plan from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!plan.Id.HasValue)
        {
            throw new DomainException($"Plan '{plan.Key}' does not have an ID and cannot be deleted.");
        }

        // Check for subscriptions before deletion (more critical than billing cycles)
        var hasSubscriptions = await _subscriptionRepository.HasSubscriptionsForPlanAsync(plan.Id.Value);
        if (hasSubscriptions)
        {
            throw new DomainException(
                $"Cannot delete plan '{plan.Key}'. Plan has active subscriptions. Please cancel or expire all subscriptions first."
            );
        }

        // Check for billing cycles before deletion
        var hasBillingCycles = await _planRepository.HasBillingCyclesAsync(plan.Id.Value);
        if (hasBillingCycles)
        {
            throw new DomainException(
                $"Cannot delete plan '{plan.Key}'. Plan has associated billing cycles. Please delete or archive all billing cycles first."
            );
        }

        await _planRepository.DeleteAsync(plan.Id.Value);
    }

    public async Task SetFeatureValueAsync(string planKey, string featureKey, string value)
    {
        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            throw new NotFoundException($"Plan with key '{planKey}' not found");
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

        plan.SetFeatureValue(feature.Id.Value, value);
        await _planRepository.SaveAsync(plan);
    }

    public async Task RemoveFeatureValueAsync(string planKey, string featureKey)
    {
        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            throw new NotFoundException($"Plan with key '{planKey}' not found");
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

        plan.RemoveFeatureValue(feature.Id.Value);
        await _planRepository.SaveAsync(plan);
    }

    public async Task<string?> GetFeatureValueAsync(string planKey, string featureKey)
    {
        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            throw new NotFoundException($"Plan with key '{planKey}' not found");
        }

        var feature = await _featureRepository.FindByKeyAsync(featureKey);
        if (feature == null)
        {
            return null;
        }

        // Feature from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!feature.Id.HasValue)
        {
            return null;
        }

        return plan.GetFeatureValue(feature.Id.Value);
    }

    public async Task<IReadOnlyList<PlanFeatureDto>> GetPlanFeaturesAsync(string planKey)
    {
        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            throw new NotFoundException($"Plan with key '{planKey}' not found");
        }

        // Map feature IDs to keys
        var features = new List<PlanFeatureDto>();
        foreach (var fv in plan.Props.FeatureValues ?? new List<PlanFeatureValue>())
        {
            var feature = await _featureRepository.FindByIdAsync(fv.FeatureId);
            if (feature != null)
            {
                features.Add(new PlanFeatureDto { FeatureKey = feature.Key, Value = fv.Value });
            }
        }

        return features;
    }
}

