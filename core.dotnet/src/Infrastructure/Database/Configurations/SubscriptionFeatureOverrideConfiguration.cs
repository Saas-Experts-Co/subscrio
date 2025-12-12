using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Database.Extensions;

namespace Subscrio.Core.Infrastructure.Database.Configurations;

public class SubscriptionFeatureOverrideConfiguration : IEntityTypeConfiguration<SubscriptionFeatureOverrideEntity>
{
    private readonly DatabaseType _databaseType;

    public SubscriptionFeatureOverrideConfiguration(DatabaseType databaseType)
    {
        _databaseType = databaseType;
    }

    public void Configure(EntityTypeBuilder<SubscriptionFeatureOverrideEntity> builder)
    {
        builder.ToTable("subscription_feature_overrides");

        builder.HasKey(sfo => sfo.Id);
        builder.Property(sfo => sfo.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(sfo => sfo.SubscriptionId)
            .HasColumnName("subscription_id")
            .IsRequired();

        builder.Property(sfo => sfo.FeatureId)
            .HasColumnName("feature_id")
            .IsRequired();

        builder.Property(sfo => sfo.Value)
            .HasColumnName("value")
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(sfo => sfo.OverrideType)
            .HasColumnName("override_type")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sfo => sfo.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);

        // Foreign keys
        builder.HasOne(sfo => sfo.Subscription)
            .WithMany()
            .HasForeignKey(sfo => sfo.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sfo => sfo.Feature)
            .WithMany()
            .HasForeignKey(sfo => sfo.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on (subscription_id, feature_id)
        builder.HasIndex(sfo => new { sfo.SubscriptionId, sfo.FeatureId })
            .IsUnique();
    }
}

