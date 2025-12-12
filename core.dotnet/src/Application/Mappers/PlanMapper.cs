using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Application.Mappers;

public static class PlanMapper
{
    public static PlanDto ToDto(
        Plan plan,
        string productKey,
        string? onExpireTransitionToBillingCycleKey = null
    )
    {
        return new PlanDto
        {
            ProductKey = productKey,
            Key = plan.Key,
            DisplayName = plan.DisplayName,
            Description = plan.Props.Description,
            Status = plan.Status.ToString().ToLowerInvariant(),
            OnExpireTransitionToBillingCycleKey = onExpireTransitionToBillingCycleKey,
            Metadata = plan.Props.Metadata,
            CreatedAt = plan.Props.CreatedAt.ToString("O"),
            UpdatedAt = plan.Props.UpdatedAt.ToString("O")
        };
    }

    public static Plan ToDomain(dynamic raw, List<PlanFeatureValue> featureValues)
    {
        // Repository should join to provide product_key and on_expire_transition_to_billing_cycle_key
        // raw.product_key comes from join with products table
        // raw.on_expire_transition_to_billing_cycle_key comes from join with billing_cycles table
        return new Plan(
            new PlanProps
            {
                ProductKey = raw.product_key, // From join, not from plans table
                Key = raw.key,
                DisplayName = raw.display_name,
                Description = raw.description,
                Status = Enum.Parse<PlanStatus>(raw.status.ToString(), true),
                OnExpireTransitionToBillingCycleKey = raw.on_expire_transition_to_billing_cycle_key, // From join, not from plans table
                FeatureValues = featureValues,
                Metadata = raw.metadata != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(System.Text.Json.JsonSerializer.Serialize(raw.metadata)) : null,
                CreatedAt = DateTime.Parse(raw.created_at.ToString()),
                UpdatedAt = DateTime.Parse(raw.updated_at.ToString())
            },
            raw.id != null ? (long?)Convert.ToInt64(raw.id) : null
        );
    }

    public static Dictionary<string, object?> ToPersistence(
        Plan plan,
        long productId,
        long? onExpireTransitionToBillingCycleId = null
    )
    {
        // Repository should resolve productKey to productId before calling this
        // Repository should resolve onExpireTransitionToBillingCycleKey to onExpireTransitionToBillingCycleId before calling this
        var record = new Dictionary<string, object?>
        {
            ["product_id"] = productId,
            ["key"] = plan.Key,
            ["display_name"] = plan.DisplayName,
            ["description"] = plan.Props.Description,
            ["status"] = plan.Status.ToString().ToLowerInvariant(),
            ["on_expire_transition_to_billing_cycle_id"] = onExpireTransitionToBillingCycleId,
            ["metadata"] = plan.Props.Metadata,
            ["created_at"] = plan.Props.CreatedAt,
            ["updated_at"] = plan.Props.UpdatedAt
        };

        // Only include id for updates (not inserts)
        if (plan.Id.HasValue)
        {
            record["id"] = plan.Id.Value;
        }

        return record;
    }
}

