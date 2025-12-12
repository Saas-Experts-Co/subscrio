using Microsoft.EntityFrameworkCore;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database;
using Subscrio.Core.Infrastructure.Database.Entities;

namespace Subscrio.Core.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IBillingCycleRepository
/// Equivalent to TypeScript DrizzleBillingCycleRepository
/// </summary>
public class EfBillingCycleRepository : IBillingCycleRepository
{
    private readonly SubscrioDbContext _dbContext;

    public EfBillingCycleRepository(SubscrioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BillingCycle> SaveAsync(BillingCycle billingCycle)
    {
        if (billingCycle.Id == null)
        {
            // Insert new entity
            var entity = new BillingCycleEntity
            {
                PlanId = billingCycle.PlanId ?? throw new InvalidOperationException("PlanId is required"),
                Key = billingCycle.Key,
                DisplayName = billingCycle.DisplayName,
                Description = billingCycle.Props.Description,
                Status = billingCycle.Status.ToString().ToLower(),
                DurationValue = billingCycle.Props.DurationValue,
                DurationUnit = billingCycle.Props.DurationUnit.ToString().ToLower(),
                ExternalProductId = billingCycle.Props.ExternalProductId,
                CreatedAt = billingCycle.Props.CreatedAt,
                UpdatedAt = billingCycle.Props.UpdatedAt
            };

            _dbContext.BillingCycles.Add(entity);
            await _dbContext.SaveChangesAsync();

            return new BillingCycle(billingCycle.Props, entity.Id);
        }
        else
        {
            // Update existing entity
            var entity = await _dbContext.BillingCycles.FindAsync(billingCycle.Id.Value);
            if (entity == null)
            {
                throw new InvalidOperationException($"BillingCycle with id {billingCycle.Id} not found");
            }

            entity.PlanId = billingCycle.PlanId ?? throw new InvalidOperationException("PlanId is required");
            entity.Key = billingCycle.Key;
            entity.DisplayName = billingCycle.DisplayName;
            entity.Description = billingCycle.Props.Description;
            entity.Status = billingCycle.Status.ToString().ToLower();
            entity.DurationValue = billingCycle.Props.DurationValue;
            entity.DurationUnit = billingCycle.Props.DurationUnit.ToString().ToLower();
            entity.ExternalProductId = billingCycle.Props.ExternalProductId;
            entity.UpdatedAt = billingCycle.Props.UpdatedAt;

            await _dbContext.SaveChangesAsync();
            return billingCycle;
        }
    }

    public async Task<BillingCycle?> FindByIdAsync(long id)
    {
        var record = await _dbContext.BillingCycles.FindAsync(id);
        if (record == null) return null;

        dynamic raw = new
        {
            id = record.Id,
            plan_id = record.PlanId,
            key = record.Key,
            display_name = record.DisplayName,
            description = record.Description,
            status = record.Status,
            duration_value = record.DurationValue,
            duration_unit = record.DurationUnit,
            external_product_id = record.ExternalProductId,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt
        };

        return BillingCycleMapper.ToDomain(raw);
    }

    public async Task<BillingCycle?> FindByKeyAsync(string key)
    {
        var record = await _dbContext.BillingCycles
            .FirstOrDefaultAsync(bc => bc.Key == key);

        if (record == null) return null;

        dynamic raw = new
        {
            id = record.Id,
            plan_id = record.PlanId,
            key = record.Key,
            display_name = record.DisplayName,
            description = record.Description,
            status = record.Status,
            duration_value = record.DurationValue,
            duration_unit = record.DurationUnit,
            external_product_id = record.ExternalProductId,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt
        };

        return BillingCycleMapper.ToDomain(raw);
    }

    public async Task<IReadOnlyList<BillingCycle>> FindByPlanAsync(long planId)
    {
        var records = await _dbContext.BillingCycles
            .Where(bc => bc.PlanId == planId)
            .OrderBy(bc => bc.CreatedAt)
            .ToListAsync();

        return records.Select(r =>
        {
            dynamic raw = new
            {
                id = r.Id,
                plan_id = r.PlanId,
                key = r.Key,
                display_name = r.DisplayName,
                description = r.Description,
                status = r.Status,
                duration_value = r.DurationValue,
                duration_unit = r.DurationUnit,
                external_product_id = r.ExternalProductId,
                created_at = r.CreatedAt,
                updated_at = r.UpdatedAt
            };
            return BillingCycleMapper.ToDomain(raw);
        }).ToList();
    }

    public async Task<IReadOnlyList<BillingCycle>> FindAllAsync(BillingCycleFilterDto? filters = null)
    {
        var query = _dbContext.BillingCycles.AsQueryable();

        if (filters != null)
        {
            if (filters.Status.HasValue)
            {
                query = query.Where(bc => bc.Status == filters.Status.Value.ToString().ToLower());
            }

            if (!string.IsNullOrEmpty(filters.DurationUnit))
            {
                query = query.Where(bc => bc.DurationUnit == filters.DurationUnit.ToLower());
            }

            if (!string.IsNullOrEmpty(filters.Search))
            {
                var search = filters.Search;
                query = query.Where(bc =>
                    (bc.DisplayName != null && bc.DisplayName.Contains(search)) ||
                    (bc.Description != null && bc.Description.Contains(search)));
            }

            // Apply sorting
            var sortBy = filters.SortBy ?? "displayOrder";
            var sortOrder = filters.SortOrder ?? "asc";

            query = sortBy switch
            {
                "displayName" => sortOrder == "desc"
                    ? query.OrderByDescending(bc => bc.DisplayName)
                    : query.OrderBy(bc => bc.DisplayName),
                _ => sortOrder == "desc"
                    ? query.OrderByDescending(bc => bc.CreatedAt)
                    : query.OrderBy(bc => bc.CreatedAt)
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
            query = query.OrderBy(bc => bc.CreatedAt);
        }

        var records = await query.ToListAsync();
        return records.Select(r =>
        {
            dynamic raw = new
            {
                id = r.Id,
                plan_id = r.PlanId,
                key = r.Key,
                display_name = r.DisplayName,
                description = r.Description,
                status = r.Status,
                duration_value = r.DurationValue,
                duration_unit = r.DurationUnit,
                external_product_id = r.ExternalProductId,
                created_at = r.CreatedAt,
                updated_at = r.UpdatedAt
            };
            return BillingCycleMapper.ToDomain(raw);
        }).ToList();
    }

    public async Task<IReadOnlyList<BillingCycle>> FindByDurationUnitAsync(DurationUnit durationUnit)
    {
        var records = await _dbContext.BillingCycles
            .Where(bc => bc.DurationUnit == durationUnit.ToString().ToLower())
            .OrderBy(bc => bc.CreatedAt)
            .ToListAsync();

        return records.Select(r =>
        {
            dynamic raw = new
            {
                id = r.Id,
                plan_id = r.PlanId,
                key = r.Key,
                display_name = r.DisplayName,
                description = r.Description,
                status = r.Status,
                duration_value = r.DurationValue,
                duration_unit = r.DurationUnit,
                external_product_id = r.ExternalProductId,
                created_at = r.CreatedAt,
                updated_at = r.UpdatedAt
            };
            return BillingCycleMapper.ToDomain(raw);
        }).ToList();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _dbContext.BillingCycles.FindAsync(id);
        if (entity != null)
        {
            _dbContext.BillingCycles.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _dbContext.BillingCycles.AnyAsync(bc => bc.Id == id);
    }
}

