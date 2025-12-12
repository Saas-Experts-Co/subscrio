using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Database.Extensions;

namespace Subscrio.Core.Infrastructure.Database.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    private readonly DatabaseType _databaseType;

    public ProductConfiguration(DatabaseType databaseType)
    {
        _databaseType = databaseType;
    }

    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Key)
            .HasColumnName("key")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(p => p.Key)
            .IsUnique();

        builder.Property(p => p.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Metadata)
            .HasColumnName("metadata")
            .AsJsonColumn(_databaseType);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);
    }
}

