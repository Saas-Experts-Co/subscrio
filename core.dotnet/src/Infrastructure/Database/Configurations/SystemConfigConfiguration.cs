using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Database.Extensions;

namespace Subscrio.Core.Infrastructure.Database.Configurations;

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfigEntity>
{
    private readonly DatabaseType _databaseType;

    public SystemConfigConfiguration(DatabaseType databaseType)
    {
        _databaseType = databaseType;
    }

    public void Configure(EntityTypeBuilder<SystemConfigEntity> builder)
    {
        builder.ToTable("system_config");

        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(sc => sc.ConfigKey)
            .HasColumnName("config_key")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(sc => sc.ConfigKey)
            .IsUnique();

        builder.Property(sc => sc.ConfigValue)
            .HasColumnName("config_value")
            .IsRequired();

        builder.Property(sc => sc.Encrypted)
            .HasColumnName("encrypted")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sc => sc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);

        builder.Property(sc => sc.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);
    }
}

