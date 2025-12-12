using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Database.Extensions;

namespace Subscrio.Core.Infrastructure.Database.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<PlanEntity>
{
    private readonly DatabaseType _databaseType;

    public PlanConfiguration(DatabaseType databaseType)
    {
        _databaseType = databaseType;
    }

    public void Configure(EntityTypeBuilder<PlanEntity> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(p => p.Key)
            .HasColumnName("key")
            .IsRequired()
            .HasMaxLength(255);

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

        builder.Property(p => p.OnExpireTransitionToBillingCycleId)
            .HasColumnName("on_expire_transition_to_billing_cycle_id");

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

        // Foreign keys
        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.OnExpireTransitionToBillingCycle)
            .WithMany()
            .HasForeignKey(p => p.OnExpireTransitionToBillingCycleId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deletion if referenced

        // Unique constraint on (product_id, key)
        builder.HasIndex(p => new { p.ProductId, p.Key })
            .IsUnique();
    }
}

