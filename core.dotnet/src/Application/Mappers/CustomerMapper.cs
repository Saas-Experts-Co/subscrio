using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Application.Mappers;

public static class CustomerMapper
{
    public static CustomerDto ToDto(Customer customer)
    {
        return new CustomerDto
        {
            Key = customer.Key,
            DisplayName = customer.Props.DisplayName,
            Email = customer.Props.Email,
            ExternalBillingId = customer.ExternalBillingId,
            Status = customer.Status.ToString().ToLowerInvariant(),
            Metadata = customer.Props.Metadata,
            CreatedAt = customer.Props.CreatedAt.ToString("O"),
            UpdatedAt = customer.Props.UpdatedAt.ToString("O")
        };
    }

    public static Customer ToDomain(dynamic raw)
    {
        return new Customer(
            new CustomerProps
            {
                Key = raw.key,
                DisplayName = raw.display_name,
                Email = raw.email,
                ExternalBillingId = raw.external_billing_id,
                Status = Enum.Parse<CustomerStatus>(raw.status.ToString(), true),
                Metadata = raw.metadata != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(System.Text.Json.JsonSerializer.Serialize(raw.metadata)) : null,
                CreatedAt = DateTime.Parse(raw.created_at.ToString()),
                UpdatedAt = DateTime.Parse(raw.updated_at.ToString())
            },
            raw.id != null ? (long?)Convert.ToInt64(raw.id) : null
        );
    }

    public static Dictionary<string, object?> ToPersistence(Customer customer)
    {
        var record = new Dictionary<string, object?>
        {
            ["key"] = customer.Key,
            ["display_name"] = customer.Props.DisplayName,
            ["email"] = customer.Props.Email,
            ["external_billing_id"] = customer.ExternalBillingId,
            ["status"] = customer.Status.ToString().ToLowerInvariant(),
            ["metadata"] = customer.Props.Metadata,
            ["created_at"] = customer.Props.CreatedAt,
            ["updated_at"] = customer.Props.UpdatedAt
        };

        // Only include id for updates (not inserts)
        if (customer.Id.HasValue)
        {
            record["id"] = customer.Id.Value;
        }

        return record;
    }
}

