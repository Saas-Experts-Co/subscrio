using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Application.Mappers;

public static class ProductMapper
{
    public static ProductDto ToDto(Product product)
    {
        return new ProductDto
        {
            Key = product.Key,
            DisplayName = product.DisplayName,
            Description = product.Props.Description,
            Status = product.Status.ToString().ToLowerInvariant(),
            Metadata = product.Props.Metadata,
            CreatedAt = product.Props.CreatedAt.ToString("O"),
            UpdatedAt = product.Props.UpdatedAt.ToString("O")
        };
    }

    public static Product ToDomain(dynamic raw)
    {
        return new Product(
            new ProductProps
            {
                Key = raw.key,
                DisplayName = raw.display_name,
                Description = raw.description,
                Status = Enum.Parse<ProductStatus>(raw.status.ToString(), true),
                Metadata = raw.metadata != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(System.Text.Json.JsonSerializer.Serialize(raw.metadata)) : null,
                CreatedAt = DateTime.Parse(raw.created_at.ToString()),
                UpdatedAt = DateTime.Parse(raw.updated_at.ToString())
            },
            raw.id != null ? (long?)Convert.ToInt64(raw.id) : null
        );
    }

    public static Dictionary<string, object?> ToPersistence(Product product)
    {
        var record = new Dictionary<string, object?>
        {
            ["key"] = product.Key,
            ["display_name"] = product.DisplayName,
            ["description"] = product.Props.Description,
            ["status"] = product.Status.ToString().ToLowerInvariant(),
            ["metadata"] = product.Props.Metadata,
            ["created_at"] = product.Props.CreatedAt,
            ["updated_at"] = product.Props.UpdatedAt
        };

        // Only include id for updates (not inserts)
        if (product.Id.HasValue)
        {
            record["id"] = product.Id.Value;
        }

        return record;
    }
}

