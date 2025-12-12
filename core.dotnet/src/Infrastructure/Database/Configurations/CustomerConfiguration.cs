using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Database.Extensions;

namespace Subscrio.Core.Infrastructure.Database.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    private readonly DatabaseType _databaseType;

    public CustomerConfiguration(DatabaseType databaseType)
    {
        _databaseType = databaseType;
    }

    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Key)
            .HasColumnName("key")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(c => c.Key)
            .IsUnique();

        builder.Property(c => c.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(255);

        builder.Property(c => c.Email)
            .HasColumnName("email")
            .HasMaxLength(255);

        builder.Property(c => c.ExternalBillingId)
            .HasColumnName("external_billing_id")
            .HasMaxLength(255);

        builder.HasIndex(c => c.ExternalBillingId)
            .IsUnique()
            .HasFilter("[external_billing_id] IS NOT NULL"); // SQL Server syntax, PostgreSQL uses WHERE

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Metadata)
            .HasColumnName("metadata")
            .AsJsonColumn(_databaseType);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .WithDefaultTimestamp(_databaseType);
    }
}

