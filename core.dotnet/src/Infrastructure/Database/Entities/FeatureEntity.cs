namespace Subscrio.Core.Infrastructure.Database.Entities;

/// <summary>
/// EF Core entity for features table
/// </summary>
public class FeatureEntity
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValueType { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object>? Validator { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

