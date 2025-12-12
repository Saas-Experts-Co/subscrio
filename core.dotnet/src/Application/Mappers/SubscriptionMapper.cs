using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Application.Mappers;

public static class SubscriptionMapper
{
    public static SubscriptionDto ToDto(
        Subscription subscription,
        string customerKey,
        string productKey,
        string planKey,
        string billingCycleKey,
        Customer? customer = null
    )
    {
        return new SubscriptionDto
        {
            Key = subscription.Key,
            CustomerKey = customerKey,
            ProductKey = productKey,
            PlanKey = planKey,
            BillingCycleKey = billingCycleKey,
            Status = subscription.Status.ToString().ToLowerInvariant(),
            IsArchived = subscription.IsArchived,
            ActivationDate = subscription.Props.ActivationDate?.ToString("O"),
            ExpirationDate = subscription.Props.ExpirationDate?.ToString("O"),
            CancellationDate = subscription.Props.CancellationDate?.ToString("O"),
            TrialEndDate = subscription.Props.TrialEndDate?.ToString("O"),
            CurrentPeriodStart = subscription.Props.CurrentPeriodStart?.ToString("O"),
            CurrentPeriodEnd = subscription.Props.CurrentPeriodEnd?.ToString("O"),
            StripeSubscriptionId = subscription.Props.StripeSubscriptionId,
            Metadata = subscription.Props.Metadata,
            Customer = customer != null ? CustomerMapper.ToDto(customer) : null,
            CreatedAt = subscription.Props.CreatedAt.ToString("O"),
            UpdatedAt = subscription.Props.UpdatedAt.ToString("O")
        };
    }

    public static Subscription ToDomain(dynamic raw, List<FeatureOverride> featureOverrides)
    {
        return new Subscription(
            new SubscriptionProps
            {
                Key = raw.key,
                CustomerId = Convert.ToInt64(raw.customer_id),
                PlanId = Convert.ToInt64(raw.plan_id),
                BillingCycleId = Convert.ToInt64(raw.billing_cycle_id),
                Status = Enum.Parse<SubscriptionStatus>(raw.computed_status.ToString(), true),
                IsArchived = raw.is_archived == true,
                ActivationDate = raw.activation_date != null ? DateTime.Parse(raw.activation_date.ToString()) : null,
                ExpirationDate = raw.expiration_date != null ? DateTime.Parse(raw.expiration_date.ToString()) : null,
                CancellationDate = raw.cancellation_date != null ? DateTime.Parse(raw.cancellation_date.ToString()) : null,
                TrialEndDate = raw.trial_end_date != null ? DateTime.Parse(raw.trial_end_date.ToString()) : null,
                CurrentPeriodStart = raw.current_period_start != null ? DateTime.Parse(raw.current_period_start.ToString()) : null,
                CurrentPeriodEnd = raw.current_period_end != null ? DateTime.Parse(raw.current_period_end.ToString()) : null,
                StripeSubscriptionId = raw.stripe_subscription_id,
                FeatureOverrides = featureOverrides,
                Metadata = raw.metadata != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(System.Text.Json.JsonSerializer.Serialize(raw.metadata)) : null,
                CreatedAt = DateTime.Parse(raw.created_at.ToString()),
                UpdatedAt = DateTime.Parse(raw.updated_at.ToString()),
                TransitionedAt = raw.transitioned_at != null ? DateTime.Parse(raw.transitioned_at.ToString()) : null
            },
            raw.id != null ? (long?)Convert.ToInt64(raw.id) : null
        );
    }

    public static Dictionary<string, object?> ToPersistence(Subscription subscription)
    {
        var record = new Dictionary<string, object?>
        {
            ["key"] = subscription.Key,
            ["customer_id"] = subscription.CustomerId,
            ["plan_id"] = subscription.PlanId,
            ["billing_cycle_id"] = subscription.Props.BillingCycleId,
            ["is_archived"] = subscription.Props.IsArchived,
            ["activation_date"] = subscription.Props.ActivationDate,
            ["expiration_date"] = subscription.Props.ExpirationDate,
            ["cancellation_date"] = subscription.Props.CancellationDate,
            ["trial_end_date"] = subscription.Props.TrialEndDate,
            ["current_period_start"] = subscription.Props.CurrentPeriodStart,
            ["current_period_end"] = subscription.Props.CurrentPeriodEnd,
            ["stripe_subscription_id"] = subscription.Props.StripeSubscriptionId,
            ["metadata"] = subscription.Props.Metadata,
            ["created_at"] = subscription.Props.CreatedAt,
            ["updated_at"] = subscription.Props.UpdatedAt,
            ["transitioned_at"] = subscription.Props.TransitionedAt
        };

        // Only include id for updates (not inserts)
        if (subscription.Id.HasValue)
        {
            record["id"] = subscription.Id.Value;
        }

        return record;
    }
}

