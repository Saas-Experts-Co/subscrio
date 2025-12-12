using Microsoft.EntityFrameworkCore;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Infrastructure.Database;
using Subscrio.Core.Infrastructure.Database.Entities;

namespace Subscrio.Core.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of ICustomerRepository
/// Equivalent to TypeScript DrizzleCustomerRepository
/// </summary>
public class EfCustomerRepository : ICustomerRepository
{
    private readonly SubscrioDbContext _dbContext;

    public EfCustomerRepository(SubscrioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Customer> SaveAsync(Customer customer)
    {
        if (customer.Id == null)
        {
            // Insert new entity
            var entity = new CustomerEntity
            {
                Key = customer.Key,
                DisplayName = customer.Props.DisplayName,
                Email = customer.Props.Email,
                ExternalBillingId = customer.Props.ExternalBillingId,
                Status = customer.Status.ToString().ToLower(),
                Metadata = customer.Props.Metadata,
                CreatedAt = customer.Props.CreatedAt,
                UpdatedAt = customer.Props.UpdatedAt
            };

            _dbContext.Customers.Add(entity);
            await _dbContext.SaveChangesAsync();

            return new Customer(customer.Props, entity.Id);
        }
        else
        {
            // Update existing entity
            var entity = await _dbContext.Customers.FindAsync(customer.Id.Value);
            if (entity == null)
            {
                throw new InvalidOperationException($"Customer with id {customer.Id} not found");
            }

            entity.Key = customer.Key;
            entity.DisplayName = customer.Props.DisplayName;
            entity.Email = customer.Props.Email;
            entity.ExternalBillingId = customer.Props.ExternalBillingId;
            entity.Status = customer.Status.ToString().ToLower();
            entity.Metadata = customer.Props.Metadata;
            entity.UpdatedAt = customer.Props.UpdatedAt;

            await _dbContext.SaveChangesAsync();
            return customer;
        }
    }

    public async Task<Customer?> FindByIdAsync(long id)
    {
        var record = await _dbContext.Customers.FindAsync(id);
        if (record == null) return null;

        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            display_name = record.DisplayName,
            email = record.Email,
            external_billing_id = record.ExternalBillingId,
            status = record.Status,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt
        };

        return CustomerMapper.ToDomain(raw);
    }

    public async Task<Customer?> FindByKeyAsync(string key)
    {
        var record = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Key == key);

        if (record == null) return null;

        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            display_name = record.DisplayName,
            email = record.Email,
            external_billing_id = record.ExternalBillingId,
            status = record.Status,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt
        };

        return CustomerMapper.ToDomain(raw);
    }

    public async Task<Customer?> FindByExternalBillingIdAsync(string externalBillingId)
    {
        var record = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.ExternalBillingId == externalBillingId);

        if (record == null) return null;

        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            display_name = record.DisplayName,
            email = record.Email,
            external_billing_id = record.ExternalBillingId,
            status = record.Status,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt
        };

        return CustomerMapper.ToDomain(raw);
    }

    public async Task<IReadOnlyList<Customer>> FindAllAsync(CustomerFilterDto? filters = null)
    {
        var query = _dbContext.Customers.AsQueryable();

        if (filters != null)
        {
            if (filters.Status.HasValue)
            {
                query = query.Where(c => c.Status == filters.Status.Value.ToString().ToLower());
            }

            if (!string.IsNullOrEmpty(filters.Search))
            {
                var search = filters.Search;
                query = query.Where(c =>
                    c.Key.Contains(search) ||
                    (c.DisplayName != null && c.DisplayName.Contains(search)) ||
                    (c.Email != null && c.Email.Contains(search)));
            }

            // Apply sorting
            var sortBy = filters.SortBy ?? "createdAt";
            var sortOrder = filters.SortOrder ?? "desc";

            query = sortBy switch
            {
                "displayName" => sortOrder == "desc"
                    ? query.OrderByDescending(c => c.DisplayName)
                    : query.OrderBy(c => c.DisplayName),
                "key" => sortOrder == "desc"
                    ? query.OrderByDescending(c => c.Key)
                    : query.OrderBy(c => c.Key),
                _ => sortOrder == "desc"
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt)
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
            query = query.OrderByDescending(c => c.CreatedAt);
        }

        var records = await query.ToListAsync();
        return records.Select(r =>
        {
            dynamic raw = new
            {
                id = r.Id,
                key = r.Key,
                display_name = r.DisplayName,
                email = r.Email,
                external_billing_id = r.ExternalBillingId,
                status = r.Status,
                metadata = r.Metadata,
                created_at = r.CreatedAt,
                updated_at = r.UpdatedAt
            };
            return CustomerMapper.ToDomain(raw);
        }).ToList();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _dbContext.Customers.FindAsync(id);
        if (entity != null)
        {
            _dbContext.Customers.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _dbContext.Customers.AnyAsync(c => c.Id == id);
    }
}

