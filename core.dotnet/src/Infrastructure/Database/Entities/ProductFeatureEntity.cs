namespace Subscrio.Core.Infrastructure.Database.Entities;

/// <summary>
/// EF Core entity for product_features junction table
/// </summary>
public class ProductFeatureEntity
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long FeatureId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ProductEntity Product { get; set; } = null!;
    public FeatureEntity Feature { get; set; } = null!;
}

