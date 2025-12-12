using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;

namespace Subscrio.Core.Application.Repositories;

public interface IFeatureRepository
{
    Task<Feature> SaveAsync(Feature feature);
    Task<Feature?> FindByIdAsync(long id);
    Task<Feature?> FindByKeyAsync(string key);
    Task<List<Feature>> FindAllAsync(FeatureFilterDto? filters = null);
    Task<List<Feature>> FindByIdsAsync(List<long> ids);
    Task DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);

    // Get features by product
    Task<List<Feature>> FindByProductAsync(long productId);

    // Foreign key checks
    Task<bool> HasProductAssociationsAsync(long featureId);
    Task<bool> HasPlanFeatureValuesAsync(long featureId);
    Task<bool> HasSubscriptionOverridesAsync(long featureId);
}

