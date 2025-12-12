using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;

namespace Subscrio.Core.Application.Repositories;

public interface ICustomerRepository
{
    Task<Customer> SaveAsync(Customer customer);
    Task<Customer?> FindByIdAsync(long id);
    Task<Customer?> FindByKeyAsync(string key);
    Task<Customer?> FindByExternalBillingIdAsync(string externalBillingId);
    Task<List<Customer>> FindAllAsync(CustomerFilterDto? filters = null);
    Task DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);
}

