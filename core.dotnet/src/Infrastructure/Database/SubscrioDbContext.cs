using Microsoft.EntityFrameworkCore;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Configurations;
using Subscrio.Core.Infrastructure.Database.Entities;

namespace Subscrio.Core.Infrastructure.Database;

/// <summary>
/// EF Core DbContext for Subscrio with support for PostgreSQL and SQL Server
/// </summary>
public class SubscrioDbContext : DbContext
{
    private readonly DatabaseType _databaseType;
    private readonly string _connectionString;

    public SubscrioDbContext(DatabaseType databaseType, string connectionString)
    {
        _databaseType = databaseType;
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        switch (_databaseType)
        {
            case DatabaseType.PostgreSQL:
                optionsBuilder.UseNpgsql(_connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "subscrio");
                });
                break;
            case DatabaseType.SqlServer:
                optionsBuilder.UseSqlServer(_connectionString, sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsHistoryTable("__EFMigrationsHistory", "subscrio");
                });
                break;
            default:
                throw new ArgumentException($"Unsupported database type: {_databaseType}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema for PostgreSQL (SQL Server will use dbo by default)
        if (_databaseType == DatabaseType.PostgreSQL)
        {
            modelBuilder.HasDefaultSchema("subscrio");
        }

        // Apply entity configurations (pass DatabaseType for database-specific column types)
        modelBuilder.ApplyConfiguration(new ProductConfiguration(_databaseType));
        modelBuilder.ApplyConfiguration(new FeatureConfiguration(_databaseType));
        modelBuilder.ApplyConfiguration(new ProductFeatureConfiguration(_databaseType));
        modelBuilder.ApplyConfiguration(new PlanConfiguration(_databaseType));
        modelBuilder.ApplyConfiguration(new PlanFeatureValueConfiguration(_databaseType));
        modelBuilder.ApplyConfiguration(new CustomerConfiguration(_databaseType));
        modelBuilder.ApplyConfiguration(new SubscriptionConfiguration(_databaseType));
        modelBuilder.ApplyConfiguration(new SubscriptionFeatureOverrideConfiguration(_databaseType));
        modelBuilder.ApplyConfiguration(new BillingCycleConfiguration(_databaseType));
        modelBuilder.ApplyConfiguration(new SystemConfigConfiguration(_databaseType));
    }

    // DbSets for all entities
    public DbSet<ProductEntity> Products { get; set; } = null!;
    public DbSet<FeatureEntity> Features { get; set; } = null!;
    public DbSet<ProductFeatureEntity> ProductFeatures { get; set; } = null!;
    public DbSet<PlanEntity> Plans { get; set; } = null!;
    public DbSet<PlanFeatureValueEntity> PlanFeatureValues { get; set; } = null!;
    public DbSet<CustomerEntity> Customers { get; set; } = null!;
    public DbSet<SubscriptionEntity> Subscriptions { get; set; } = null!;
    public DbSet<SubscriptionFeatureOverrideEntity> SubscriptionFeatureOverrides { get; set; } = null!;
    public DbSet<BillingCycleEntity> BillingCycles { get; set; } = null!;
    public DbSet<SystemConfigEntity> SystemConfigs { get; set; } = null!;
}

