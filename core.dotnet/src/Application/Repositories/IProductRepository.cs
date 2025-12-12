using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;

namespace Subscrio.Core.Application.Repositories;

public interface IProductRepository
{
    Task<Product> SaveAsync(Product product);
    Task<Product?> FindByIdAsync(long id);
    Task<Product?> FindByKeyAsync(string key);
    Task<List<Product>> FindAllAsync(ProductFilterDto? filters = null);
    Task DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);

    // Product-Feature associations
    Task AssociateFeatureAsync(long productId, long featureId);
    Task DissociateFeatureAsync(long productId, long featureId);
    Task<List<long>> GetFeaturesByProductAsync(long productId);

    // Foreign key checks
    Task<bool> HasPlansAsync(string productKey);
}

