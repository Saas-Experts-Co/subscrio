namespace Subscrio.Core.Infrastructure.Database.Entities;

/// <summary>
/// EF Core entity for products table
/// </summary>
public class ProductEntity
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

