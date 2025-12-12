namespace Subscrio.Core.Infrastructure.Database.Entities;

/// <summary>
/// EF Core entity for system_config table
/// </summary>
public class SystemConfigEntity
{
    public long Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public bool Encrypted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

