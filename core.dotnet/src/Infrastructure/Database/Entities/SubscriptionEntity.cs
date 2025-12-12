namespace Subscrio.Core.Infrastructure.Database.Entities;

/// <summary>
/// EF Core entity for subscriptions table
/// </summary>
public class SubscriptionEntity
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public long PlanId { get; set; }
    public long BillingCycleId { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ActivationDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? CancellationDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? TransitionedAt { get; set; }

    // Navigation properties
    public CustomerEntity Customer { get; set; } = null!;
    public PlanEntity Plan { get; set; } = null!;
    public BillingCycleEntity BillingCycle { get; set; } = null!;
}

