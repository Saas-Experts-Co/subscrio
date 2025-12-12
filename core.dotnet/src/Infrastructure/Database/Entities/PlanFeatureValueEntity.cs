namespace Subscrio.Core.Infrastructure.Database.Entities;

/// <summary>
/// EF Core entity for plan_features table
/// </summary>
public class PlanFeatureValueEntity
{
    public long Id { get; set; }
    public long PlanId { get; set; }
    public long FeatureId { get; set; }
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public PlanEntity Plan { get; set; } = null!;
    public FeatureEntity Feature { get; set; } = null!;
}

