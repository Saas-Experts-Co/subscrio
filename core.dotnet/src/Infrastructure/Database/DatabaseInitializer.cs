using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Utils;

namespace Subscrio.Core.Infrastructure.Database;

/// <summary>
/// Database schema installer and initializer
/// Equivalent to TypeScript installer.ts SchemaInstaller class
/// </summary>
public class DatabaseInitializer
{
    private readonly SubscrioDbContext _dbContext;
    private readonly DatabaseType _databaseType;
    private readonly ILogger<DatabaseInitializer>? _logger;
    private const string CURRENT_SCHEMA_VERSION = "1.1.0";

    public DatabaseInitializer(SubscrioDbContext dbContext, DatabaseType databaseType, ILogger<DatabaseInitializer>? logger = null)
    {
        _dbContext = dbContext;
        _databaseType = databaseType;
        _logger = logger;
    }

    /// <summary>
    /// Install database schema
    /// Equivalent to installer.ts install() method
    /// </summary>
    public async Task InstallAsync(string? adminPassphrase = null)
    {
        // Ensure database exists
        await EnsureDatabaseExistsAsync();

        // Create schema (PostgreSQL only)
        if (_databaseType == DatabaseType.PostgreSQL)
        {
            await CreateSchemaIfNotExistsAsync();
        }

        // Run migrations (EF Core handles table creation)
        await _dbContext.Database.MigrateAsync();

        // Setup initial system configuration
        await SetupInitialConfigAsync(adminPassphrase);
    }

    /// <summary>
    /// Verify schema installation
    /// Returns the schema version if installed, null otherwise
    /// Equivalent to installer.ts verify() method
    /// </summary>
    public async Task<string?> VerifyAsync()
    {
        return await GetCurrentSchemaVersionAsync();
    }

    /// <summary>
    /// Get current schema version from system_config
    /// Equivalent to installer.ts getCurrentSchemaVersion() method
    /// </summary>
    public async Task<string?> GetCurrentSchemaVersionAsync()
    {
        try
        {
            var record = await _dbContext.SystemConfigs
                .FirstOrDefaultAsync(sc => sc.ConfigKey == "schema_version");

            return record?.ConfigValue;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Run pending migrations
    /// Returns the number of migrations applied
    /// Equivalent to installer.ts migrate() method
    /// </summary>
    public async Task<int> MigrateAsync()
    {
        var currentVersion = await GetCurrentSchemaVersionAsync();
        var migrationsApplied = 0;

        // Migration 1.1.0: Add transitioned_at column to subscriptions
        // This is handled by EF Core migrations, but we track the version
        if (string.IsNullOrEmpty(currentVersion) || CompareVersions(currentVersion, CURRENT_SCHEMA_VERSION) < 0)
        {
            // EF Core migrations will handle the actual schema changes
            await _dbContext.Database.MigrateAsync();
            await UpdateSchemaVersionAsync(CURRENT_SCHEMA_VERSION);
            migrationsApplied++;
        }

        return migrationsApplied;
    }

    /// <summary>
    /// Drop all Subscrio tables (in reverse dependency order)
    /// Equivalent to installer.ts dropAll() method
    /// </summary>
    public async Task DropAllAsync()
    {
        // Drop view first (PostgreSQL only)
        if (_databaseType == DatabaseType.PostgreSQL)
        {
            await _dbContext.Database.ExecuteSqlRawAsync("DROP VIEW IF EXISTS subscrio.subscription_status_view CASCADE");
        }

        // Drop tables in reverse dependency order
        var tablesToDrop = new[]
        {
            "subscription_feature_overrides",
            "subscriptions",
            "plan_features",
            "plans",
            "product_features",
            "features",
            "products",
            "customers",
            "billing_cycles",
            "system_config"
        };

        foreach (var table in tablesToDrop)
        {
            var schemaPrefix = _databaseType == DatabaseType.PostgreSQL ? "subscrio." : "";
            await _dbContext.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {schemaPrefix}{table} CASCADE");
        }
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        try
        {
            await _dbContext.Database.CanConnectAsync();
        }
        catch
        {
            _logger?.LogWarning("Database does not exist or cannot be connected. Please ensure the database exists.");
            throw;
        }
    }

    private async Task CreateSchemaIfNotExistsAsync()
    {
        if (_databaseType == DatabaseType.PostgreSQL)
        {
            await _dbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS subscrio");
        }
    }

    /// <summary>
    /// Setup initial system configuration
    /// Equivalent to installer.ts setupInitialConfig() method
    /// </summary>
    private async Task SetupInitialConfigAsync(string? adminPassphrase)
    {
        // Check if admin passphrase already exists
        var existing = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(sc => sc.ConfigKey == "admin_passphrase_hash");

        if (existing == null && !string.IsNullOrEmpty(adminPassphrase))
        {
            // Hash the admin passphrase
            var hash = BCrypt.Net.BCrypt.HashPassword(adminPassphrase);

            // Insert into system_config
            var config = new SystemConfigEntity
            {
                ConfigKey = "admin_passphrase_hash",
                ConfigValue = hash,
                Encrypted = false,
                CreatedAt = DateHelper.Now(),
                UpdatedAt = DateHelper.Now()
            };

            _dbContext.SystemConfigs.Add(config);
            await _dbContext.SaveChangesAsync();
        }

        // Set initial schema version if not exists
        var versionCheck = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(sc => sc.ConfigKey == "schema_version");

        if (versionCheck == null)
        {
            var versionConfig = new SystemConfigEntity
            {
                ConfigKey = "schema_version",
                ConfigValue = CURRENT_SCHEMA_VERSION,
                Encrypted = false,
                CreatedAt = DateHelper.Now(),
                UpdatedAt = DateHelper.Now()
            };

            _dbContext.SystemConfigs.Add(versionConfig);
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Update schema version in system_config
    /// Equivalent to installer.ts updateSchemaVersion() method
    /// </summary>
    private async Task UpdateSchemaVersionAsync(string version)
    {
        var existing = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(sc => sc.ConfigKey == "schema_version");

        if (existing != null)
        {
            existing.ConfigValue = version;
            existing.UpdatedAt = DateHelper.Now();
            _dbContext.SystemConfigs.Update(existing);
        }
        else
        {
            var versionConfig = new SystemConfigEntity
            {
                ConfigKey = "schema_version",
                ConfigValue = version,
                Encrypted = false,
                CreatedAt = DateHelper.Now(),
                UpdatedAt = DateHelper.Now()
            };

            _dbContext.SystemConfigs.Add(versionConfig);
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Compare two version strings (e.g., "1.0.0" vs "1.1.0")
    /// Returns: -1 if v1 < v2, 0 if v1 == v2, 1 if v1 > v2
    /// Equivalent to installer.ts compareVersions() method
    /// </summary>
    private int CompareVersions(string v1, string v2)
    {
        var parts1 = v1.Split('.').Select(int.Parse).ToArray();
        var parts2 = v2.Split('.').Select(int.Parse).ToArray();

        var maxLength = Math.Max(parts1.Length, parts2.Length);

        for (int i = 0; i < maxLength; i++)
        {
            var part1 = i < parts1.Length ? parts1[i] : 0;
            var part2 = i < parts2.Length ? parts2[i] : 0;

            if (part1 < part2) return -1;
            if (part1 > part2) return 1;
        }

        return 0;
    }
}

