namespace Subscrio.Core.Infrastructure.Database.Entities;

/// <summary>
/// EF Core entity for subscription_feature_overrides table
/// </summary>
public class SubscriptionFeatureOverrideEntity
{
    public long Id { get; set; }
    public long SubscriptionId { get; set; }
    public long FeatureId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string OverrideType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public SubscriptionEntity Subscription { get; set; } = null!;
    public FeatureEntity Feature { get; set; } = null!;
}

