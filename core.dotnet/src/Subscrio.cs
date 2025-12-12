using Microsoft.Extensions.Logging;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Application.Services;
using Subscrio.Core.Config;
using Subscrio.Core.Domain.Services;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database;
using Subscrio.Core.Infrastructure.Repositories;

namespace Subscrio.Core;

/// <summary>
/// Main Subscrio class - entry point for the library
/// Equivalent to TypeScript Subscrio.ts
/// </summary>
public class Subscrio : IDisposable
{
    private readonly SubscrioDbContext _dbContext;
    private readonly DatabaseInitializer _installer;
    private readonly ILogger<Subscrio>? _logger;

    // Repositories (private)
    private readonly IProductRepository _productRepo;
    private readonly IFeatureRepository _featureRepo;
    private readonly IPlanRepository _planRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IBillingCycleRepository _billingCycleRepo;

    // Public services
    public ProductManagementService Products { get; }
    public FeatureManagementService Features { get; }
    public PlanManagementService Plans { get; }
    public CustomerManagementService Customers { get; }
    public SubscriptionManagementService Subscriptions { get; }
    public BillingCycleManagementService BillingCycles { get; }
    public FeatureCheckerService FeatureChecker { get; }
    public StripeIntegrationService? Stripe { get; }
    public ConfigSyncService ConfigSync { get; }

    public Subscrio(SubscrioConfig config, ILogger<Subscrio>? logger = null)
    {
        _logger = logger;

        // Initialize database
        _dbContext = new SubscrioDbContext(
            config.Database.DatabaseType,
            config.Database.ConnectionString
        );
        _installer = new DatabaseInitializer(_dbContext, config.Database.DatabaseType, logger);

        // Initialize repositories
        _productRepo = new EfProductRepository(_dbContext);
        _featureRepo = new EfFeatureRepository(_dbContext);
        _planRepo = new EfPlanRepository(_dbContext);
        _customerRepo = new EfCustomerRepository(_dbContext);
        _subscriptionRepo = new EfSubscriptionRepository(_dbContext);
        _billingCycleRepo = new EfBillingCycleRepository(_dbContext);

        // Initialize domain services
        // FeatureValueResolver is instantiated within FeatureCheckerService

        // Initialize application services
        Products = new ProductManagementService(
            _productRepo,
            _featureRepo
        );
        Features = new FeatureManagementService(
            _featureRepo,
            _productRepo
        );
        Plans = new PlanManagementService(
            _planRepo,
            _productRepo,
            _featureRepo,
            _subscriptionRepo
        );
        Customers = new CustomerManagementService(_customerRepo);
        Subscriptions = new SubscriptionManagementService(
            _subscriptionRepo,
            _customerRepo,
            _planRepo,
            _billingCycleRepo,
            _featureRepo,
            _productRepo
        );
        BillingCycles = new BillingCycleManagementService(
            _billingCycleRepo,
            _planRepo,
            _subscriptionRepo
        );
        FeatureChecker = new FeatureCheckerService(
            _subscriptionRepo,
            _planRepo,
            _featureRepo,
            _customerRepo,
            _productRepo
        );

        // Initialize Stripe service if configured
        // Note: StripeIntegrationService will be implemented in Phase 5
        // For now, Stripe is null if not configured
        Stripe = null; // TODO: Implement in Phase 5

        ConfigSync = new ConfigSyncService(this);
    }

    /// <summary>
    /// Install database schema
    /// Equivalent to TypeScript installSchema() method
    /// </summary>
    public async Task InstallSchemaAsync(string? adminPassphrase = null)
    {
        await _installer.InstallAsync(adminPassphrase);
    }

    /// <summary>
    /// Verify schema installation
    /// Returns the schema version if installed, null otherwise
    /// Equivalent to TypeScript verifySchema() method
    /// </summary>
    public async Task<string?> VerifySchemaAsync()
    {
        return await _installer.VerifyAsync();
    }

    /// <summary>
    /// Run pending database migrations
    /// 
    /// Migrations are tracked via schema_version in system_config.
    /// This method runs only pending migrations and updates the version.
    /// 
    /// Equivalent to TypeScript migrate() method
    /// </summary>
    /// <returns>Number of migrations applied</returns>
    public async Task<int> MigrateAsync()
    {
        return await _installer.MigrateAsync();
    }

    /// <summary>
    /// Drop all database tables (WARNING: Destructive!)
    /// Equivalent to TypeScript dropSchema() method
    /// </summary>
    public async Task DropSchemaAsync()
    {
        await _installer.DropAllAsync();
    }

    /// <summary>
    /// Close database connections
    /// Equivalent to TypeScript close() method
    /// </summary>
    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}

