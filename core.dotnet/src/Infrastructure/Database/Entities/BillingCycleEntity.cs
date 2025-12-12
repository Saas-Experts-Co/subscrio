namespace Subscrio.Core.Infrastructure.Database.Entities;

/// <summary>
/// EF Core entity for billing_cycles table
/// </summary>
public class BillingCycleEntity
{
    public long Id { get; set; }
    public long PlanId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? DurationValue { get; set; }
    public string DurationUnit { get; set; } = string.Empty;
    public string? ExternalProductId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public PlanEntity Plan { get; set; } = null!;
}

