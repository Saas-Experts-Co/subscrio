using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Application.Mappers;

public static class FeatureMapper
{
    public static FeatureDto ToDto(Feature feature)
    {
        return new FeatureDto
        {
            Key = feature.Key,
            DisplayName = feature.DisplayName,
            Description = feature.Props.Description,
            ValueType = feature.ValueType.ToString().ToLowerInvariant(),
            DefaultValue = feature.DefaultValue,
            GroupName = feature.Props.GroupName,
            Status = feature.Status.ToString().ToLowerInvariant(),
            Validator = feature.Props.Validator,
            Metadata = feature.Props.Metadata,
            CreatedAt = feature.Props.CreatedAt.ToString("O"),
            UpdatedAt = feature.Props.UpdatedAt.ToString("O")
        };
    }

    public static Feature ToDomain(dynamic raw)
    {
        return new Feature(
            new FeatureProps
            {
                Key = raw.key,
                DisplayName = raw.display_name,
                Description = raw.description,
                ValueType = Enum.Parse<FeatureValueType>(raw.value_type.ToString(), true),
                DefaultValue = raw.default_value,
                GroupName = raw.group_name,
                Status = Enum.Parse<FeatureStatus>(raw.status.ToString(), true),
                Validator = raw.validator != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(System.Text.Json.JsonSerializer.Serialize(raw.validator)) : null,
                Metadata = raw.metadata != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(System.Text.Json.JsonSerializer.Serialize(raw.metadata)) : null,
                CreatedAt = DateTime.Parse(raw.created_at.ToString()),
                UpdatedAt = DateTime.Parse(raw.updated_at.ToString())
            },
            raw.id != null ? (long?)Convert.ToInt64(raw.id) : null
        );
    }

    public static Dictionary<string, object?> ToPersistence(Feature feature)
    {
        var record = new Dictionary<string, object?>
        {
            ["key"] = feature.Key,
            ["display_name"] = feature.DisplayName,
            ["description"] = feature.Props.Description,
            ["value_type"] = feature.ValueType.ToString().ToLowerInvariant(),
            ["default_value"] = feature.DefaultValue,
            ["group_name"] = feature.Props.GroupName,
            ["status"] = feature.Status.ToString().ToLowerInvariant(),
            ["validator"] = feature.Props.Validator,
            ["metadata"] = feature.Props.Metadata,
            ["created_at"] = feature.Props.CreatedAt,
            ["updated_at"] = feature.Props.UpdatedAt
        };

        // Only include id for updates (not inserts)
        if (feature.Id.HasValue)
        {
            record["id"] = feature.Id.Value;
        }

        return record;
    }
}

