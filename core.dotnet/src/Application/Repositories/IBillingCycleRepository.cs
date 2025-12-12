using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;

namespace Subscrio.Core.Application.Repositories;

public interface IBillingCycleRepository
{
    Task<BillingCycle> SaveAsync(BillingCycle billingCycle);
    Task<BillingCycle?> FindByIdAsync(long id);
    Task<BillingCycle?> FindByKeyAsync(string key);
    Task<List<BillingCycle>> FindByPlanAsync(long planId);
    Task<List<BillingCycle>> FindAllAsync(BillingCycleFilterDto? filters = null);
    Task DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);
}

