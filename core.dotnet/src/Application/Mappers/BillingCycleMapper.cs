using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Application.Mappers;

public static class BillingCycleMapper
{
    public static BillingCycleDto ToDto(BillingCycle billingCycle, string? productKey = null, string? planKey = null)
    {
        return new BillingCycleDto
        {
            ProductKey = productKey,
            PlanKey = planKey,
            Key = billingCycle.Key,
            DisplayName = billingCycle.DisplayName,
            Description = billingCycle.Props.Description,
            Status = billingCycle.Status.ToString().ToLowerInvariant(),
            DurationValue = billingCycle.Props.DurationValue,
            DurationUnit = billingCycle.Props.DurationUnit.ToString().ToLowerInvariant(),
            ExternalProductId = billingCycle.Props.ExternalProductId,
            CreatedAt = billingCycle.Props.CreatedAt.ToString("O"),
            UpdatedAt = billingCycle.Props.UpdatedAt.ToString("O")
        };
    }

    public static BillingCycle ToDomain(dynamic raw)
    {
        return new BillingCycle(
            new BillingCycleProps
            {
                PlanId = Convert.ToInt64(raw.plan_id),
                Key = raw.key,
                DisplayName = raw.display_name,
                Description = raw.description,
                Status = Enum.TryParse<BillingCycleStatus>(raw.status.ToString(), true, out var status) ? status : BillingCycleStatus.Active,
                DurationValue = raw.duration_value != null ? Convert.ToInt32(raw.duration_value) : null,
                DurationUnit = Enum.Parse<DurationUnit>(raw.duration_unit.ToString(), true),
                ExternalProductId = raw.external_product_id,
                CreatedAt = DateTime.Parse(raw.created_at.ToString()),
                UpdatedAt = DateTime.Parse(raw.updated_at.ToString())
            },
            raw.id != null ? (long?)Convert.ToInt64(raw.id) : null
        );
    }

    public static Dictionary<string, object?> ToPersistence(BillingCycle billingCycle)
    {
        var record = new Dictionary<string, object?>
        {
            ["plan_id"] = billingCycle.PlanId,
            ["key"] = billingCycle.Key,
            ["display_name"] = billingCycle.DisplayName,
            ["description"] = billingCycle.Props.Description,
            ["status"] = billingCycle.Status.ToString().ToLowerInvariant(),
            ["duration_value"] = billingCycle.Props.DurationValue,
            ["duration_unit"] = billingCycle.Props.DurationUnit.ToString().ToLowerInvariant(),
            ["external_product_id"] = billingCycle.Props.ExternalProductId,
            ["created_at"] = billingCycle.Props.CreatedAt,
            ["updated_at"] = billingCycle.Props.UpdatedAt
        };

        // Only include id for updates (not inserts)
        if (billingCycle.Id.HasValue)
        {
            record["id"] = billingCycle.Id.Value;
        }

        return record;
    }
}

