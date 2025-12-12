using FluentValidation;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Utils;

namespace Subscrio.Core.Application.Services;

public class BillingCycleManagementService
{
    private readonly IBillingCycleRepository _billingCycleRepository;
    private readonly IPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IValidator<CreateBillingCycleDto> _createBillingCycleValidator;
    private readonly IValidator<UpdateBillingCycleDto> _updateBillingCycleValidator;
    private readonly IValidator<BillingCycleFilterDto> _billingCycleFilterValidator;

    public BillingCycleManagementService(
        IBillingCycleRepository billingCycleRepository,
        IPlanRepository planRepository,
        ISubscriptionRepository subscriptionRepository,
        IValidator<CreateBillingCycleDto> createBillingCycleValidator,
        IValidator<UpdateBillingCycleDto> updateBillingCycleValidator,
        IValidator<BillingCycleFilterDto> billingCycleFilterValidator)
    {
        _billingCycleRepository = billingCycleRepository;
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _createBillingCycleValidator = createBillingCycleValidator;
        _updateBillingCycleValidator = updateBillingCycleValidator;
        _billingCycleFilterValidator = billingCycleFilterValidator;
    }

    public async Task<BillingCycleDto> CreateBillingCycleAsync(CreateBillingCycleDto dto)
    {
        // Validate input
        var validationResult = await _createBillingCycleValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                $"Invalid billing cycle data for key '{dto.Key}': {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                validationResult.Errors
            );
        }

        // Verify plan exists
        var plan = await _planRepository.FindByKeyAsync(dto.PlanKey);
        if (plan == null)
        {
            throw new NotFoundException($"Plan with key '{dto.PlanKey}' not found");
        }

        // Check if key already exists globally
        var existing = await _billingCycleRepository.FindByKeyAsync(dto.Key);
        if (existing != null)
        {
            throw new ConflictException($"Billing cycle with key '{dto.Key}' already exists");
        }

        // Validate duration unit
        if (!Enum.IsDefined(typeof(DurationUnit), dto.DurationUnit))
        {
            throw new ValidationException($"Invalid duration unit: {dto.DurationUnit}");
        }

        // Validate duration value based on duration unit
        if (dto.DurationUnit == DurationUnit.Forever)
        {
            // For forever billing cycles, durationValue must be undefined
            if (dto.DurationValue != null)
            {
                throw new ValidationException("Duration value must not be provided for forever billing cycles");
            }
        }
        else
        {
            // For all other duration units, durationValue is required and must be positive
            if (dto.DurationValue == null)
            {
                throw new ValidationException("Duration value is required for non-forever billing cycles");
            }
            if (dto.DurationValue <= 0)
            {
                throw new ValidationException("Duration value must be greater than 0");
            }
        }

        // Plan from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!plan.Id.HasValue)
        {
            throw new DomainException($"Plan '{plan.Key}' does not have an ID.");
        }

        // Create domain entity (no ID - database will generate)
        var billingCycle = new BillingCycle(new BillingCycleProps(
            plan.Id.Value,
            dto.Key,
            dto.DisplayName,
            dto.Description,
            BillingCycleStatus.Active,
            dto.DurationValue,
            dto.DurationUnit,
            dto.ExternalProductId,
            DateHelper.Now(),
            DateHelper.Now()
        ));

        // Save and get entity with generated ID
        var savedBillingCycle = await _billingCycleRepository.SaveAsync(billingCycle);
        return BillingCycleMapper.ToDto(savedBillingCycle, plan.ProductKey, plan.Key);
    }

    public async Task<BillingCycleDto> UpdateBillingCycleAsync(string key, UpdateBillingCycleDto dto)
    {
        // Validate input
        var validationResult = await _updateBillingCycleValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Invalid update data", validationResult.Errors);
        }

        var billingCycle = await _billingCycleRepository.FindByKeyAsync(key);
        if (billingCycle == null)
        {
            throw new NotFoundException($"Billing cycle with key '{key}' not found");
        }

        // Update properties
        if (dto.DisplayName != null)
        {
            billingCycle.Props = billingCycle.Props with { DisplayName = dto.DisplayName };
        }
        if (dto.Description != null)
        {
            billingCycle.Props = billingCycle.Props with { Description = dto.Description };
        }
        if (dto.DurationValue != null)
        {
            if (dto.DurationValue <= 0)
            {
                throw new ValidationException("Duration value must be greater than 0");
            }
            billingCycle.Props = billingCycle.Props with { DurationValue = dto.DurationValue };
        }
        if (dto.DurationUnit != null)
        {
            if (!Enum.IsDefined(typeof(DurationUnit), dto.DurationUnit.Value))
            {
                throw new ValidationException($"Invalid duration unit: {dto.DurationUnit}");
            }
            billingCycle.Props = billingCycle.Props with { DurationUnit = dto.DurationUnit.Value };
        }
        if (dto.ExternalProductId != null)
        {
            billingCycle.Props = billingCycle.Props with { ExternalProductId = dto.ExternalProductId };
        }

        billingCycle.Props = billingCycle.Props with { UpdatedAt = DateHelper.Now() };
        await _billingCycleRepository.SaveAsync(billingCycle);

        // Get plan to resolve keys for DTO
        var plan = await _planRepository.FindByIdAsync(billingCycle.Props.PlanId);
        if (plan == null)
        {
            throw new NotFoundException("Plan not found for billing cycle");
        }

        return BillingCycleMapper.ToDto(billingCycle, plan.ProductKey, plan.Key);
    }

    public async Task<BillingCycleDto?> GetBillingCycleAsync(string key)
    {
        var billingCycle = await _billingCycleRepository.FindByKeyAsync(key);
        if (billingCycle == null)
        {
            return null;
        }

        // Get plan to resolve keys for DTO
        var plan = await _planRepository.FindByIdAsync(billingCycle.Props.PlanId);
        if (plan == null)
        {
            throw new NotFoundException("Plan not found for billing cycle");
        }

        return BillingCycleMapper.ToDto(billingCycle, plan.ProductKey, plan.Key);
    }

    public async Task<IReadOnlyList<BillingCycleDto>> GetBillingCyclesByPlanAsync(string planKey)
    {
        // Verify plan exists
        var plan = await _planRepository.FindByKeyAsync(planKey);
        if (plan == null)
        {
            throw new NotFoundException($"Plan with key '{planKey}' not found");
        }

        // Plan from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!plan.Id.HasValue)
        {
            throw new DomainException($"Plan '{plan.Key}' does not have an ID.");
        }

        var billingCycles = await _billingCycleRepository.FindByPlanAsync(plan.Id.Value);
        return billingCycles.Select(bc => BillingCycleMapper.ToDto(bc, plan.ProductKey, plan.Key)).ToList();
    }

    public async Task<IReadOnlyList<BillingCycleDto>> ListBillingCyclesAsync(BillingCycleFilterDto? filters = null)
    {
        filters ??= new BillingCycleFilterDto();
        var validationResult = await _billingCycleFilterValidator.ValidateAsync(filters);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Invalid filter parameters", validationResult.Errors);
        }

        // If filtering by plan, get plan first
        if (validationResult.Data.PlanKey != null)
        {
            return await GetBillingCyclesByPlanAsync(validationResult.Data.PlanKey);
        }

        // Otherwise list all (need to resolve productKey/planKey for each)
        var billingCycles = await _billingCycleRepository.FindAllAsync(validationResult.Data);
        var dtos = new List<BillingCycleDto>();

        foreach (var bc in billingCycles)
        {
            var plan = await _planRepository.FindByIdAsync(bc.Props.PlanId);
            if (plan != null)
            {
                dtos.Add(BillingCycleMapper.ToDto(bc, plan.ProductKey, plan.Key));
            }
        }

        return dtos;
    }

    public async Task ArchiveBillingCycleAsync(string key)
    {
        var billingCycle = await _billingCycleRepository.FindByKeyAsync(key);
        if (billingCycle == null)
        {
            throw new NotFoundException($"Billing cycle with key '{key}' not found");
        }

        billingCycle.Archive();
        await _billingCycleRepository.SaveAsync(billingCycle);
    }

    public async Task UnarchiveBillingCycleAsync(string key)
    {
        var billingCycle = await _billingCycleRepository.FindByKeyAsync(key);
        if (billingCycle == null)
        {
            throw new NotFoundException($"Billing cycle with key '{key}' not found");
        }

        billingCycle.Unarchive();
        await _billingCycleRepository.SaveAsync(billingCycle);
    }

    public async Task DeleteBillingCycleAsync(string key)
    {
        var billingCycle = await _billingCycleRepository.FindByKeyAsync(key);
        if (billingCycle == null)
        {
            throw new NotFoundException($"Billing cycle with key '{key}' not found");
        }

        if (!billingCycle.CanDelete())
        {
            throw new DomainException(
                $"Cannot delete billing cycle with status '{billingCycle.Status}'. Billing cycle must be archived before deletion."
            );
        }

        // Billing cycle from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!billingCycle.Id.HasValue)
        {
            throw new DomainException($"Billing cycle '{billingCycle.Key}' does not have an ID and cannot be deleted.");
        }

        // Check for subscriptions before deletion
        var hasSubscriptions = await _subscriptionRepository.HasSubscriptionsForBillingCycleAsync(billingCycle.Id.Value);
        if (hasSubscriptions)
        {
            throw new DomainException(
                $"Cannot delete billing cycle '{billingCycle.Key}'. Billing cycle has active subscriptions. Please cancel or expire all subscriptions first."
            );
        }

        // Check for plan transition references
        var hasPlanTransitionReferences = await _planRepository.HasPlanTransitionReferencesAsync(billingCycle.Key);
        if (hasPlanTransitionReferences)
        {
            throw new DomainException(
                $"Cannot delete billing cycle '{billingCycle.Key}'. Billing cycle is referenced by plan transition settings. Please update or remove plan transition references first."
            );
        }

        await _billingCycleRepository.DeleteAsync(billingCycle.Id.Value);
    }

    /// <summary>
    /// Calculate next period end date based on billing cycle
    /// </summary>
    public async Task<DateTime?> CalculateNextPeriodEndAsync(string billingCycleKey, DateTime currentPeriodEnd)
    {
        var billingCycle = await _billingCycleRepository.FindByKeyAsync(billingCycleKey);
        if (billingCycle == null)
        {
            throw new NotFoundException($"Billing cycle with key '{billingCycleKey}' not found");
        }
        return billingCycle.CalculateNextPeriodEnd(currentPeriodEnd);
    }

    /// <summary>
    /// Get billing cycles by duration unit
    /// </summary>
    public async Task<IReadOnlyList<BillingCycleDto>> GetBillingCyclesByDurationUnitAsync(DurationUnit durationUnit)
    {
        var allCycles = await _billingCycleRepository.FindAllAsync();
        var filtered = allCycles.Where(cycle => cycle.Props.DurationUnit == durationUnit).ToList();
        
        var dtos = new List<BillingCycleDto>();
        foreach (var cycle in filtered)
        {
            var plan = await _planRepository.FindByIdAsync(cycle.Props.PlanId);
            if (plan != null)
            {
                dtos.Add(BillingCycleMapper.ToDto(cycle, plan.ProductKey, plan.Key));
            }
        }
        return dtos;
    }

    /// <summary>
    /// Get default billing cycles (commonly used ones)
    /// </summary>
    public async Task<IReadOnlyList<BillingCycleDto>> GetDefaultBillingCyclesAsync()
    {
        var commonCycles = new[]
        {
            new { Key = "monthly", DurationValue = (int?)1, DurationUnit = DurationUnit.Months },
            new { Key = "quarterly", DurationValue = (int?)3, DurationUnit = DurationUnit.Months },
            new { Key = "yearly", DurationValue = (int?)1, DurationUnit = DurationUnit.Years }
        };

        var cycles = new List<BillingCycleDto>();

        foreach (var cycle in commonCycles)
        {
            var existing = await _billingCycleRepository.FindByKeyAsync(cycle.Key);
            if (existing != null)
            {
                var plan = await _planRepository.FindByIdAsync(existing.Props.PlanId);
                if (plan != null)
                {
                    cycles.Add(BillingCycleMapper.ToDto(existing, plan.ProductKey, plan.Key));
                }
            }
        }

        return cycles;
    }
}

