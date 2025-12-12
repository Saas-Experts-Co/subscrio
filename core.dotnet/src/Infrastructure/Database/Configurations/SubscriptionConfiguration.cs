using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Database.Extensions;

namespace Subscrio.Core.Infrastructure.Database.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<SubscriptionEntity>
{
    private readonly DatabaseType _databaseType;

    public SubscriptionConfiguration(DatabaseType databaseType)
    {
        _databaseType = databaseType;
    }

    public void Configure(EntityTypeBuilder<SubscriptionEntity> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.Key)
            .HasColumnName("key")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(s => s.Key)
            .IsUnique();

        builder.Property(s => s.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(s => s.PlanId)
            .HasColumnName("plan_id")
            .IsRequired();

        builder.Property(s => s.BillingCycleId)
            .HasColumnName("billing_cycle_id")
            .IsRequired();

        builder.Property(s => s.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.ActivationDate)
            .HasColumnName("activation_date")
            .AsTimestampTz(_databaseType);

        builder.Property(s => s.ExpirationDate)
            .HasColumnName("expiration_date")
            .AsTimestampTz(_databaseType);

        builder.Property(s => s.CancellationDate)
            .HasColumnName("cancellation_date")
            .AsTimestampTz(_databaseType);

        builder.Property(s => s.TrialEndDate)
            .HasColumnName("trial_end_date")
            .AsTimestampTz(_databaseType);

        builder.Property(s => s.CurrentPeriodStart)
            .HasColumnName("current_period_start")
            .AsTimestampTz(_databaseType);

        builder.Property(s => s.CurrentPeriodEnd)
            .HasColumnName("current_period_end")
            .AsTimestampTz(_databaseType);

        builder.Property(s => s.StripeSubscriptionId)
            .HasColumnName("stripe_subscription_id")
            .HasMaxLength(255);

        builder.HasIndex(s => s.StripeSubscriptionId)
            .IsUnique()
            .HasFilter("[stripe_subscription_id] IS NOT NULL");

        builder.Property(s => s.Metadata)
            .HasColumnName("metadata")
            .AsJsonColumn(_databaseType);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType)
            .AsTimestampTz(_databaseType);

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType)
            .AsTimestampTz(_databaseType);

        builder.Property(s => s.TransitionedAt)
            .HasColumnName("transitioned_at")
            .AsTimestampTz(_databaseType);

        // Foreign keys
        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.BillingCycle)
            .WithMany()
            .HasForeignKey(s => s.BillingCycleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

