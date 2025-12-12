using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;

namespace Subscrio.Core.Application.Repositories;

public interface IPlanRepository
{
    Task<Plan> SaveAsync(Plan plan);
    Task<Plan?> FindByIdAsync(long id);
    Task<Plan?> FindByKeyAsync(string key);
    Task<List<Plan>> FindByProductAsync(string productKey); // Uses productKey - joins to resolve
    Task<Plan?> FindByBillingCycleIdAsync(long billingCycleId);
    Task<List<Plan>> FindAllAsync(PlanFilterDto? filters = null);
    Task<List<Plan>> FindByIdsAsync(List<long> ids);
    Task DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);

    // Foreign key checks
    Task<bool> HasBillingCyclesAsync(long planId);
    Task<bool> HasPlanTransitionReferencesAsync(string billingCycleKey);
}

