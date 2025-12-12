using Microsoft.EntityFrameworkCore;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Infrastructure.Database;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Utils;

namespace Subscrio.Core.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IProductRepository
/// Equivalent to TypeScript DrizzleProductRepository
/// </summary>
public class EfProductRepository : IProductRepository
{
    private readonly SubscrioDbContext _dbContext;

    public EfProductRepository(SubscrioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product> SaveAsync(Product product)
    {
        if (product.Id == null)
        {
            // Insert new entity
            var entity = new ProductEntity
            {
                Key = product.Key,
                DisplayName = product.DisplayName,
                Description = product.Props.Description,
                Status = product.Status.ToString().ToLower(),
                Metadata = product.Props.Metadata,
                CreatedAt = product.Props.CreatedAt,
                UpdatedAt = product.Props.UpdatedAt
            };

            _dbContext.Products.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Return entity with generated ID
            return new Product(product.Props, entity.Id);
        }
        else
        {
            // Update existing entity
            var entity = await _dbContext.Products.FindAsync(product.Id.Value);
            if (entity == null)
            {
                throw new InvalidOperationException($"Product with id {product.Id} not found");
            }

            entity.Key = product.Key;
            entity.DisplayName = product.DisplayName;
            entity.Description = product.Props.Description;
            entity.Status = product.Status.ToString().ToLower();
            entity.Metadata = product.Props.Metadata;
            entity.UpdatedAt = product.Props.UpdatedAt;

            await _dbContext.SaveChangesAsync();
            return product;
        }
    }

    public async Task<Product?> FindByIdAsync(long id)
    {
        var record = await _dbContext.Products.FindAsync(id);
        if (record == null) return null;
        
        // Convert entity to dynamic object with snake_case properties for mapper
        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            display_name = record.DisplayName,
            description = record.Description,
            status = record.Status,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt
        };
        
        return ProductMapper.ToDomain(raw);
    }

    public async Task<Product?> FindByKeyAsync(string key)
    {
        var record = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Key == key);

        if (record == null) return null;
        
        // Convert entity to dynamic object with snake_case properties for mapper
        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            display_name = record.DisplayName,
            description = record.Description,
            status = record.Status,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt
        };
        
        return ProductMapper.ToDomain(raw);
    }

    public async Task<IReadOnlyList<Product>> FindAllAsync(ProductFilterDto? filters = null)
    {
        var query = _dbContext.Products.AsQueryable();

        if (filters != null)
        {
            if (filters.Status.HasValue)
            {
                query = query.Where(p => p.Status == filters.Status.Value.ToString().ToLower());
            }

            if (!string.IsNullOrEmpty(filters.Search))
            {
                var search = filters.Search;
                query = query.Where(p =>
                    p.DisplayName.Contains(search) ||
                    p.Key.Contains(search));
            }

            // Apply sorting
            var sortBy = filters.SortBy ?? "createdAt";
            var sortOrder = filters.SortOrder ?? "asc";

            query = sortBy switch
            {
                "displayName" => sortOrder == "desc"
                    ? query.OrderByDescending(p => p.DisplayName)
                    : query.OrderBy(p => p.DisplayName),
                _ => sortOrder == "desc"
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt)
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
            query = query.OrderBy(p => p.CreatedAt);
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
                status = r.Status,
                metadata = r.Metadata,
                created_at = r.CreatedAt,
                updated_at = r.UpdatedAt
            };
            return ProductMapper.ToDomain(raw);
        }).ToList();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _dbContext.Products.FindAsync(id);
        if (entity != null)
        {
            _dbContext.Products.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _dbContext.Products.AnyAsync(p => p.Id == id);
    }

    public async Task AssociateFeatureAsync(long productId, long featureId)
    {
        // Check if association already exists
        var exists = await _dbContext.ProductFeatures
            .AnyAsync(pf => pf.ProductId == productId && pf.FeatureId == featureId);

        if (!exists)
        {
            var entity = new ProductFeatureEntity
            {
                ProductId = productId,
                FeatureId = featureId,
                CreatedAt = DateHelper.Now()
            };

            _dbContext.ProductFeatures.Add(entity);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task DissociateFeatureAsync(long productId, long featureId)
    {
        var entity = await _dbContext.ProductFeatures
            .FirstOrDefaultAsync(pf => pf.ProductId == productId && pf.FeatureId == featureId);

        if (entity != null)
        {
            _dbContext.ProductFeatures.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<long>> GetFeaturesByProductAsync(long productId)
    {
        var featureIds = await _dbContext.ProductFeatures
            .Where(pf => pf.ProductId == productId)
            .Select(pf => pf.FeatureId)
            .ToListAsync();

        return featureIds;
    }

    public async Task<bool> HasPlansAsync(string productKey)
    {
        // Join with products table to resolve key to ID
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Key == productKey);

        if (product == null) return false;

        return await _dbContext.Plans
            .AnyAsync(pl => pl.ProductId == product.Id);
    }
}

