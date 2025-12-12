using Microsoft.EntityFrameworkCore;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Infrastructure.Database;
using Subscrio.Core.Infrastructure.Database.Entities;

namespace Subscrio.Core.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IPlanRepository
/// Equivalent to TypeScript DrizzlePlanRepository
/// </summary>
public class EfPlanRepository : IPlanRepository
{
    private readonly SubscrioDbContext _dbContext;

    public EfPlanRepository(SubscrioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Plan> SaveAsync(Plan plan)
    {
        // Resolve productKey to productId
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Key == plan.ProductKey);

        if (product == null)
        {
            throw new InvalidOperationException($"Product with key '{plan.ProductKey}' not found");
        }

        // Resolve onExpireTransitionToBillingCycleKey to onExpireTransitionToBillingCycleId if provided
        long? billingCycleId = null;
        if (!string.IsNullOrEmpty(plan.Props.OnExpireTransitionToBillingCycleKey))
        {
            var billingCycle = await _dbContext.BillingCycles
                .FirstOrDefaultAsync(bc => bc.Key == plan.Props.OnExpireTransitionToBillingCycleKey);

            if (billingCycle == null)
            {
                throw new InvalidOperationException($"Billing cycle with key '{plan.Props.OnExpireTransitionToBillingCycleKey}' not found");
            }

            billingCycleId = billingCycle.Id;
        }

        long savedPlanId;
        if (plan.Id == null)
        {
            // Insert new entity
            var entity = new PlanEntity
            {
                ProductId = product.Id,
                Key = plan.Key,
                DisplayName = plan.DisplayName,
                Description = plan.Props.Description,
                Status = plan.Status.ToString().ToLower(),
                OnExpireTransitionToBillingCycleId = billingCycleId,
                Metadata = plan.Props.Metadata,
                CreatedAt = plan.Props.CreatedAt,
                UpdatedAt = plan.Props.UpdatedAt
            };

            _dbContext.Plans.Add(entity);
            await _dbContext.SaveChangesAsync();

            savedPlanId = entity.Id;

            // Insert feature values
            if (plan.Props.FeatureValues.Count > 0)
            {
                var featureValueEntities = plan.Props.FeatureValues.Select(fv => new PlanFeatureValueEntity
                {
                    PlanId = savedPlanId,
                    FeatureId = fv.FeatureId,
                    Value = fv.Value,
                    CreatedAt = fv.CreatedAt,
                    UpdatedAt = fv.UpdatedAt
                }).ToList();

                _dbContext.PlanFeatureValues.AddRange(featureValueEntities);
                await _dbContext.SaveChangesAsync();
            }

            return new Plan(plan.Props, savedPlanId);
        }
        else
        {
            // Update existing entity
            savedPlanId = plan.Id.Value;

            var entity = await _dbContext.Plans.FindAsync(savedPlanId);
            if (entity == null)
            {
                throw new InvalidOperationException($"Plan with id {savedPlanId} not found");
            }

            entity.ProductId = product.Id;
            entity.Key = plan.Key;
            entity.DisplayName = plan.DisplayName;
            entity.Description = plan.Props.Description;
            entity.Status = plan.Status.ToString().ToLower();
            entity.OnExpireTransitionToBillingCycleId = billingCycleId;
            entity.Metadata = plan.Props.Metadata;
            entity.UpdatedAt = plan.Props.UpdatedAt;

            // Delete existing feature values
            var existingFeatureValues = await _dbContext.PlanFeatureValues
                .Where(pf => pf.PlanId == savedPlanId)
                .ToListAsync();
            _dbContext.PlanFeatureValues.RemoveRange(existingFeatureValues);

            // Insert new feature values
            if (plan.Props.FeatureValues.Count > 0)
            {
                var featureValueEntities = plan.Props.FeatureValues.Select(fv => new PlanFeatureValueEntity
                {
                    PlanId = savedPlanId,
                    FeatureId = fv.FeatureId,
                    Value = fv.Value,
                    CreatedAt = fv.CreatedAt,
                    UpdatedAt = fv.UpdatedAt
                }).ToList();

                _dbContext.PlanFeatureValues.AddRange(featureValueEntities);
            }

            await _dbContext.SaveChangesAsync();
            return plan;
        }
    }

    private async Task<List<PlanFeatureValue>> LoadFeatureValuesAsync(long planId)
    {
        var records = await _dbContext.PlanFeatureValues
            .Where(pf => pf.PlanId == planId)
            .ToListAsync();

        return records.Select(r => new PlanFeatureValue
        {
            FeatureId = r.FeatureId,
            Value = r.Value,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }

    public async Task<Plan?> FindByIdAsync(long id)
    {
        var result = await _dbContext.Plans
            .Join(_dbContext.Products,
                p => p.ProductId,
                pr => pr.Id,
                (p, pr) => new { Plan = p, ProductKey = pr.Key })
            .GroupJoin(_dbContext.BillingCycles,
                x => x.Plan.OnExpireTransitionToBillingCycleId,
                bc => bc.Id,
                (x, bcs) => new { x.Plan, x.ProductKey, BillingCycle = bcs.FirstOrDefault() })
            .Where(x => x.Plan.Id == id)
            .FirstOrDefaultAsync();

        if (result == null) return null;

        var featureValues = await LoadFeatureValuesAsync(result.Plan.Id);

        dynamic raw = new
        {
            id = result.Plan.Id,
            product_key = result.ProductKey,
            key = result.Plan.Key,
            display_name = result.Plan.DisplayName,
            description = result.Plan.Description,
            status = result.Plan.Status,
            on_expire_transition_to_billing_cycle_key = result.BillingCycle?.Key,
            metadata = result.Plan.Metadata,
            created_at = result.Plan.CreatedAt,
            updated_at = result.Plan.UpdatedAt
        };

        return PlanMapper.ToDomain(raw, featureValues);
    }

    public async Task<Plan?> FindByKeyAsync(string key)
    {
        var result = await _dbContext.Plans
            .Join(_dbContext.Products,
                p => p.ProductId,
                pr => pr.Id,
                (p, pr) => new { Plan = p, ProductKey = pr.Key })
            .GroupJoin(_dbContext.BillingCycles,
                x => x.Plan.OnExpireTransitionToBillingCycleId,
                bc => bc.Id,
                (x, bcs) => new { x.Plan, x.ProductKey, BillingCycle = bcs.FirstOrDefault() })
            .Where(x => x.Plan.Key == key)
            .FirstOrDefaultAsync();

        if (result == null) return null;

        var featureValues = await LoadFeatureValuesAsync(result.Plan.Id);

        dynamic raw = new
        {
            id = result.Plan.Id,
            product_key = result.ProductKey,
            key = result.Plan.Key,
            display_name = result.Plan.DisplayName,
            description = result.Plan.Description,
            status = result.Plan.Status,
            on_expire_transition_to_billing_cycle_key = result.BillingCycle?.Key,
            metadata = result.Plan.Metadata,
            created_at = result.Plan.CreatedAt,
            updated_at = result.Plan.UpdatedAt
        };

        return PlanMapper.ToDomain(raw, featureValues);
    }

    public async Task<IReadOnlyList<Plan>> FindByProductAsync(string productKey)
    {
        var results = await _dbContext.Plans
            .Join(_dbContext.Products,
                p => p.ProductId,
                pr => pr.Id,
                (p, pr) => new { Plan = p, ProductKey = pr.Key })
            .GroupJoin(_dbContext.BillingCycles,
                x => x.Plan.OnExpireTransitionToBillingCycleId,
                bc => bc.Id,
                (x, bcs) => new { x.Plan, x.ProductKey, BillingCycle = bcs.FirstOrDefault() })
            .Where(x => x.ProductKey == productKey)
            .OrderBy(x => x.Plan.CreatedAt)
            .ToListAsync();

        var plans = new List<Plan>();
        foreach (var result in results)
        {
            var featureValues = await LoadFeatureValuesAsync(result.Plan.Id);

            dynamic raw = new
            {
                id = result.Plan.Id,
                product_key = result.ProductKey,
                key = result.Plan.Key,
                display_name = result.Plan.DisplayName,
                description = result.Plan.Description,
                status = result.Plan.Status,
                on_expire_transition_to_billing_cycle_key = result.BillingCycle?.Key,
                metadata = result.Plan.Metadata,
                created_at = result.Plan.CreatedAt,
                updated_at = result.Plan.UpdatedAt
            };

            plans.Add(PlanMapper.ToDomain(raw, featureValues));
        }

        return plans;
    }

    public async Task<IReadOnlyList<Plan>> FindAllAsync(PlanFilterDto? filters = null)
    {
        var query = _dbContext.Plans
            .Join(_dbContext.Products,
                p => p.ProductId,
                pr => pr.Id,
                (p, pr) => new { Plan = p, ProductKey = pr.Key })
            .GroupJoin(_dbContext.BillingCycles,
                x => x.Plan.OnExpireTransitionToBillingCycleId,
                bc => bc.Id,
                (x, bcs) => new { x.Plan, x.ProductKey, BillingCycle = bcs.FirstOrDefault() })
            .AsQueryable();

        if (filters != null)
        {
            if (!string.IsNullOrEmpty(filters.ProductKey))
            {
                query = query.Where(x => x.ProductKey == filters.ProductKey);
            }

            if (filters.Status.HasValue)
            {
                query = query.Where(x => x.Plan.Status == filters.Status.Value.ToString().ToLower());
            }

            if (!string.IsNullOrEmpty(filters.Search))
            {
                var search = filters.Search;
                query = query.Where(x =>
                    x.Plan.Key.Contains(search) ||
                    x.Plan.DisplayName.Contains(search) ||
                    (x.Plan.Description != null && x.Plan.Description.Contains(search)));
            }

            // Apply sorting
            var sortBy = filters.SortBy ?? "createdAt";
            var sortOrder = filters.SortOrder ?? "asc";

            query = sortBy switch
            {
                "displayName" => sortOrder == "desc"
                    ? query.OrderByDescending(x => x.Plan.DisplayName)
                    : query.OrderBy(x => x.Plan.DisplayName),
                _ => sortOrder == "desc"
                    ? query.OrderByDescending(x => x.Plan.CreatedAt)
                    : query.OrderBy(x => x.Plan.CreatedAt)
            };

            // Apply pagination
            if (filters.Limit > 0)
            {
                query = query.Take(filters.Limit);
            }
            if (filters.Offset > 0)
            {
                query = query.Skip(filters.Offset);
            }
        }
        else
        {
            query = query.OrderBy(x => x.Plan.CreatedAt);
        }

        var results = await query.ToListAsync();

        var plans = new List<Plan>();
        foreach (var result in results)
        {
            var featureValues = await LoadFeatureValuesAsync(result.Plan.Id);

            dynamic raw = new
            {
                id = result.Plan.Id,
                product_key = result.ProductKey,
                key = result.Plan.Key,
                display_name = result.Plan.DisplayName,
                description = result.Plan.Description,
                status = result.Plan.Status,
                on_expire_transition_to_billing_cycle_key = result.BillingCycle?.Key,
                metadata = result.Plan.Metadata,
                created_at = result.Plan.CreatedAt,
                updated_at = result.Plan.UpdatedAt
            };

            plans.Add(PlanMapper.ToDomain(raw, featureValues));
        }

        return plans;
    }

    public async Task<IReadOnlyList<Plan>> FindByIdsAsync(IReadOnlyList<long> ids)
    {
        if (ids.Count == 0) return new List<Plan>();

        var results = await _dbContext.Plans
            .Join(_dbContext.Products,
                p => p.ProductId,
                pr => pr.Id,
                (p, pr) => new { Plan = p, ProductKey = pr.Key })
            .GroupJoin(_dbContext.BillingCycles,
                x => x.Plan.OnExpireTransitionToBillingCycleId,
                bc => bc.Id,
                (x, bcs) => new { x.Plan, x.ProductKey, BillingCycle = bcs.FirstOrDefault() })
            .Where(x => ids.Contains(x.Plan.Id))
            .ToListAsync();

        var plans = new List<Plan>();
        foreach (var result in results)
        {
            var featureValues = await LoadFeatureValuesAsync(result.Plan.Id);

            dynamic raw = new
            {
                id = result.Plan.Id,
                product_key = result.ProductKey,
                key = result.Plan.Key,
                display_name = result.Plan.DisplayName,
                description = result.Plan.Description,
                status = result.Plan.Status,
                on_expire_transition_to_billing_cycle_key = result.BillingCycle?.Key,
                metadata = result.Plan.Metadata,
                created_at = result.Plan.CreatedAt,
                updated_at = result.Plan.UpdatedAt
            };

            plans.Add(PlanMapper.ToDomain(raw, featureValues));
        }

        return plans;
    }

    public async Task<Plan?> FindByBillingCycleIdAsync(long billingCycleId)
    {
        var billingCycle = await _dbContext.BillingCycles.FindAsync(billingCycleId);
        if (billingCycle == null) return null;

        return await FindByIdAsync(billingCycle.PlanId);
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _dbContext.Plans.FindAsync(id);
        if (entity != null)
        {
            _dbContext.Plans.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _dbContext.Plans.AnyAsync(p => p.Id == id);
    }

    public async Task<bool> HasBillingCyclesAsync(long planId)
    {
        return await _dbContext.BillingCycles
            .AnyAsync(bc => bc.PlanId == planId);
    }

    public async Task<bool> HasPlanTransitionReferencesAsync(string billingCycleKey)
    {
        var billingCycle = await _dbContext.BillingCycles
            .FirstOrDefaultAsync(bc => bc.Key == billingCycleKey);

        if (billingCycle == null) return false;

        return await _dbContext.Plans
            .AnyAsync(p => p.OnExpireTransitionToBillingCycleId == billingCycle.Id);
    }
}

