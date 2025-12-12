using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Database.Extensions;

namespace Subscrio.Core.Infrastructure.Database.Configurations;

public class FeatureConfiguration : IEntityTypeConfiguration<FeatureEntity>
{
    private readonly DatabaseType _databaseType;

    public FeatureConfiguration(DatabaseType databaseType)
    {
        _databaseType = databaseType;
    }

    public void Configure(EntityTypeBuilder<FeatureEntity> builder)
    {
        builder.ToTable("features");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(f => f.Key)
            .HasColumnName("key")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(f => f.Key)
            .IsUnique();

        builder.Property(f => f.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(f => f.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(f => f.ValueType)
            .HasColumnName("value_type")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.DefaultValue)
            .HasColumnName("default_value")
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(f => f.GroupName)
            .HasColumnName("group_name")
            .HasMaxLength(255);

        builder.Property(f => f.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.Validator)
            .HasColumnName("validator")
            .AsJsonColumn(_databaseType);

        builder.Property(f => f.Metadata)
            .HasColumnName("metadata")
            .AsJsonColumn(_databaseType);

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);

        builder.Property(f => f.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);
    }
}

