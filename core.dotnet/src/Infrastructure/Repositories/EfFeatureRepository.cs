using Microsoft.EntityFrameworkCore;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Infrastructure.Database;
using Subscrio.Core.Infrastructure.Database.Entities;

namespace Subscrio.Core.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IFeatureRepository
/// Equivalent to TypeScript DrizzleFeatureRepository
/// </summary>
public class EfFeatureRepository : IFeatureRepository
{
    private readonly SubscrioDbContext _dbContext;

    public EfFeatureRepository(SubscrioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Feature> SaveAsync(Feature feature)
    {
        if (feature.Id == null)
        {
            // Insert new entity
            var entity = new FeatureEntity
            {
                Key = feature.Key,
                DisplayName = feature.DisplayName,
                Description = feature.Props.Description,
                ValueType = feature.Props.ValueType.ToString().ToLower(),
                DefaultValue = feature.Props.DefaultValue,
                GroupName = feature.Props.GroupName,
                Status = feature.Status.ToString().ToLower(),
                Validator = feature.Props.Validator,
                Metadata = feature.Props.Metadata,
                CreatedAt = feature.Props.CreatedAt,
                UpdatedAt = feature.Props.UpdatedAt
            };

            _dbContext.Features.Add(entity);
            await _dbContext.SaveChangesAsync();

            return new Feature(feature.Props, entity.Id);
        }
        else
        {
            // Update existing entity
            var entity = await _dbContext.Features.FindAsync(feature.Id.Value);
            if (entity == null)
            {
                throw new InvalidOperationException($"Feature with id {feature.Id} not found");
            }

            entity.Key = feature.Key;
            entity.DisplayName = feature.DisplayName;
            entity.Description = feature.Props.Description;
            entity.ValueType = feature.Props.ValueType.ToString().ToLower();
            entity.DefaultValue = feature.Props.DefaultValue;
            entity.GroupName = feature.Props.GroupName;
            entity.Status = feature.Status.ToString().ToLower();
            entity.Validator = feature.Props.Validator;
            entity.Metadata = feature.Props.Metadata;
            entity.UpdatedAt = feature.Props.UpdatedAt;

            await _dbContext.SaveChangesAsync();
            return feature;
        }
    }

    public async Task<Feature?> FindByIdAsync(long id)
    {
        var record = await _dbContext.Features.FindAsync(id);
        if (record == null) return null;

        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            display_name = record.DisplayName,
            description = record.Description,
            value_type = record.ValueType,
            default_value = record.DefaultValue,
            group_name = record.GroupName,
            status = record.Status,
            validator = record.Validator,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt
        };

        return FeatureMapper.ToDomain(raw);
    }

    public async Task<Feature?> FindByKeyAsync(string key)
    {
        var record = await _dbContext.Features
            .FirstOrDefaultAsync(f => f.Key == key);

        if (record == null) return null;

        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            display_name = record.DisplayName,
            description = record.Description,
            value_type = record.ValueType,
            default_value = record.DefaultValue,
            group_name = record.GroupName,
            status = record.Status,
            validator = record.Validator,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt
        };

        return FeatureMapper.ToDomain(raw);
    }

    public async Task<IReadOnlyList<Feature>> FindAllAsync(FeatureFilterDto? filters = null)
    {
        var query = _dbContext.Features.AsQueryable();

        if (filters != null)
        {
            if (filters.Status.HasValue)
            {
                query = query.Where(f => f.Status == filters.Status.Value.ToString().ToLower());
            }

            if (!string.IsNullOrEmpty(filters.ValueType))
            {
                query = query.Where(f => f.ValueType == filters.ValueType.ToLower());
            }

            if (!string.IsNullOrEmpty(filters.GroupName))
            {
                query = query.Where(f => f.GroupName == filters.GroupName);
            }

            if (!string.IsNullOrEmpty(filters.Search))
            {
                var search = filters.Search;
                query = query.Where(f =>
                    f.Key.Contains(search) ||
                    f.DisplayName.Contains(search) ||
                    (f.Description != null && f.Description.Contains(search)));
            }

            // Apply sorting
            var sortBy = filters.SortBy ?? "createdAt";
            var sortOrder = filters.SortOrder ?? "asc";

            query = sortBy switch
            {
                "displayName" => sortOrder == "desc"
                    ? query.OrderByDescending(f => f.DisplayName)
                    : query.OrderBy(f => f.DisplayName),
                _ => sortOrder == "desc"
                    ? query.OrderByDescending(f => f.CreatedAt)
                    : query.OrderBy(f => f.CreatedAt)
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
            query = query.OrderBy(f => f.CreatedAt);
        }

        var records = await query.ToListAsync();
        return records.Select(r =>
        {
            dynamic raw = new
            {
                id = r.Id,
                key = r.Key,
                display_name = r.DisplayName,
                description = r.Description,
                value_type = r.ValueType,
                default_value = r.DefaultValue,
                group_name = r.GroupName,
                status = r.Status,
                validator = r.Validator,
                metadata = r.Metadata,
                created_at = r.CreatedAt,
                updated_at = r.UpdatedAt
            };
            return FeatureMapper.ToDomain(raw);
        }).ToList();
    }

    public async Task<IReadOnlyList<Feature>> FindByIdsAsync(IReadOnlyList<long> ids)
    {
        if (ids.Count == 0) return new List<Feature>();

        var records = await _dbContext.Features
            .Where(f => ids.Contains(f.Id))
            .ToListAsync();

        return records.Select(r =>
        {
            dynamic raw = new
            {
                id = r.Id,
                key = r.Key,
                display_name = r.DisplayName,
                description = r.Description,
                value_type = r.ValueType,
                default_value = r.DefaultValue,
                group_name = r.GroupName,
                status = r.Status,
                validator = r.Validator,
                metadata = r.Metadata,
                created_at = r.CreatedAt,
                updated_at = r.UpdatedAt
            };
            return FeatureMapper.ToDomain(raw);
        }).ToList();
    }

    public async Task<IReadOnlyList<Feature>> FindByProductAsync(long productId)
    {
        var records = await _dbContext.Features
            .Join(_dbContext.ProductFeatures,
                f => f.Id,
                pf => pf.FeatureId,
                (f, pf) => new { Feature = f, ProductFeature = pf })
            .Where(x => x.ProductFeature.ProductId == productId)
            .OrderBy(x => x.Feature.CreatedAt)
            .Select(x => x.Feature)
            .ToListAsync();

        return records.Select(r =>
        {
            dynamic raw = new
            {
                id = r.Id,
                key = r.Key,
                display_name = r.DisplayName,
                description = r.Description,
                value_type = r.ValueType,
                default_value = r.DefaultValue,
                group_name = r.GroupName,
                status = r.Status,
                validator = r.Validator,
                metadata = r.Metadata,
                created_at = r.CreatedAt,
                updated_at = r.UpdatedAt
            };
            return FeatureMapper.ToDomain(raw);
        }).ToList();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _dbContext.Features.FindAsync(id);
        if (entity != null)
        {
            _dbContext.Features.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _dbContext.Features.AnyAsync(f => f.Id == id);
    }

    public async Task<bool> HasProductAssociationsAsync(long featureId)
    {
        return await _dbContext.ProductFeatures
            .AnyAsync(pf => pf.FeatureId == featureId);
    }

    public async Task<bool> HasPlanFeatureValuesAsync(long featureId)
    {
        return await _dbContext.PlanFeatureValues
            .AnyAsync(pf => pf.FeatureId == featureId);
    }

    public async Task<bool> HasSubscriptionOverridesAsync(long featureId)
    {
        return await _dbContext.SubscriptionFeatureOverrides
            .AnyAsync(sfo => sfo.FeatureId == featureId);
    }
}

