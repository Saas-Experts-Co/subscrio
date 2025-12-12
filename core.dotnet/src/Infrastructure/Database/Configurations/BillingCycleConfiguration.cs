using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Database.Extensions;

namespace Subscrio.Core.Infrastructure.Database.Configurations;

public class BillingCycleConfiguration : IEntityTypeConfiguration<BillingCycleEntity>
{
    private readonly DatabaseType _databaseType;

    public BillingCycleConfiguration(DatabaseType databaseType)
    {
        _databaseType = databaseType;
    }

    public void Configure(EntityTypeBuilder<BillingCycleEntity> builder)
    {
        builder.ToTable("billing_cycles");

        builder.HasKey(bc => bc.Id);
        builder.Property(bc => bc.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(bc => bc.PlanId)
            .HasColumnName("plan_id")
            .IsRequired();

        builder.Property(bc => bc.Key)
            .HasColumnName("key")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(bc => bc.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(bc => bc.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(bc => bc.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("active");

        builder.Property(bc => bc.DurationValue)
            .HasColumnName("duration_value");

        builder.Property(bc => bc.DurationUnit)
            .HasColumnName("duration_unit")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(bc => bc.ExternalProductId)
            .HasColumnName("external_product_id")
            .HasMaxLength(255);

        builder.Property(bc => bc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);

        builder.Property(bc => bc.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);

        // Foreign key
        builder.HasOne(bc => bc.Plan)
            .WithMany()
            .HasForeignKey(bc => bc.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on (plan_id, key)
        builder.HasIndex(bc => new { bc.PlanId, bc.Key })
            .IsUnique();
    }
}

