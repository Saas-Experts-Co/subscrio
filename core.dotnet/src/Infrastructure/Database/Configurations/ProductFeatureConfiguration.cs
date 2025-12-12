using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Database.Extensions;

namespace Subscrio.Core.Infrastructure.Database.Configurations;

public class ProductFeatureConfiguration : IEntityTypeConfiguration<ProductFeatureEntity>
{
    private readonly DatabaseType _databaseType;

    public ProductFeatureConfiguration(DatabaseType databaseType)
    {
        _databaseType = databaseType;
    }

    public void Configure(EntityTypeBuilder<ProductFeatureEntity> builder)
    {
        builder.ToTable("product_features");

        builder.HasKey(pf => pf.Id);
        builder.Property(pf => pf.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(pf => pf.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(pf => pf.FeatureId)
            .HasColumnName("feature_id")
            .IsRequired();

        builder.Property(pf => pf.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);

        // Foreign keys
        builder.HasOne(pf => pf.Product)
            .WithMany()
            .HasForeignKey(pf => pf.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pf => pf.Feature)
            .WithMany()
            .HasForeignKey(pf => pf.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on (product_id, feature_id)
        builder.HasIndex(pf => new { pf.ProductId, pf.FeatureId })
            .IsUnique();
    }
}

