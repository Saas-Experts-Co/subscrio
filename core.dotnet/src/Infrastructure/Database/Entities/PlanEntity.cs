namespace Subscrio.Core.Infrastructure.Database.Entities;

/// <summary>
/// EF Core entity for plans table
/// </summary>
public class PlanEntity
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public long? OnExpireTransitionToBillingCycleId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ProductEntity Product { get; set; } = null!;
    public BillingCycleEntity? OnExpireTransitionToBillingCycle { get; set; }
}

