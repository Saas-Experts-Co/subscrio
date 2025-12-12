namespace Subscrio.Core.Infrastructure.Database.Entities;

/// <summary>
/// EF Core entity for customers table
/// </summary>
public class CustomerEntity
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? ExternalBillingId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

