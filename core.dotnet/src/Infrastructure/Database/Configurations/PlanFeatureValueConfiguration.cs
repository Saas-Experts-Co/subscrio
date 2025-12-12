using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Database.Extensions;

namespace Subscrio.Core.Infrastructure.Database.Configurations;

public class PlanFeatureValueConfiguration : IEntityTypeConfiguration<PlanFeatureValueEntity>
{
    private readonly DatabaseType _databaseType;

    public PlanFeatureValueConfiguration(DatabaseType databaseType)
    {
        _databaseType = databaseType;
    }

    public void Configure(EntityTypeBuilder<PlanFeatureValueEntity> builder)
    {
        builder.ToTable("plan_features");

        builder.HasKey(pf => pf.Id);
        builder.Property(pf => pf.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(pf => pf.PlanId)
            .HasColumnName("plan_id")
            .IsRequired();

        builder.Property(pf => pf.FeatureId)
            .HasColumnName("feature_id")
            .IsRequired();

        builder.Property(pf => pf.Value)
            .HasColumnName("value")
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(pf => pf.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);

        builder.Property(pf => pf.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);

        // Foreign keys
        builder.HasOne(pf => pf.Plan)
            .WithMany()
            .HasForeignKey(pf => pf.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pf => pf.Feature)
            .WithMany()
            .HasForeignKey(pf => pf.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on (plan_id, feature_id)
        builder.HasIndex(pf => new { pf.PlanId, pf.FeatureId })
            .IsUnique();
    }
}

