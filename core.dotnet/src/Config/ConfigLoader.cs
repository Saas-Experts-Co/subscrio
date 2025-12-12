using FluentValidation;
using FluentValidation.Results;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Config;

/// <summary>
/// Configuration loader and validator
/// Equivalent to TypeScript config/loader.ts
/// </summary>
public static class ConfigLoader
{
    /// <summary>
    /// Load and validate configuration from environment variables
    /// Equivalent to TypeScript loadConfig() function
    /// </summary>
    public static SubscrioConfig LoadConfig()
    {
        // Determine database type from connection string or environment variable
        var databaseType = DetermineDatabaseType();

        var config = new SubscrioConfig
        {
            Database = new DatabaseConfig
            {
                ConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ?? string.Empty,
                Ssl = Environment.GetEnvironmentVariable("DATABASE_SSL")?.Equals("true", StringComparison.OrdinalIgnoreCase),
                PoolSize = int.TryParse(Environment.GetEnvironmentVariable("DATABASE_POOL_SIZE"), out var poolSize) ? poolSize : null,
                DatabaseType = databaseType
            },
            AdminPassphrase = Environment.GetEnvironmentVariable("ADMIN_PASSPHRASE"),
            Stripe = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY"))
                ? new StripeConfig
                {
                    SecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")!
                }
                : null,
            Logging = new LoggingConfig
            {
                Level = Enum.TryParse<LogLevel>(
                    Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Info",
                    ignoreCase: true,
                    out var logLevel)
                    ? logLevel
                    : LogLevel.Info
            }
        };

        // Validate configuration
        var validator = new SubscrioConfigValidator();
        var validationResult = validator.Validate(config);
        
        if (!validationResult.IsValid)
        {
            throw new ConfigurationException(
                $"Invalid configuration: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                validationResult.Errors);
        }

        return config;
    }

    private static DatabaseType DetermineDatabaseType()
    {
        // Check for explicit database type environment variable
        if (Enum.TryParse<DatabaseType>(
            Environment.GetEnvironmentVariable("DATABASE_TYPE") ?? string.Empty,
            ignoreCase: true,
            out var dbType))
        {
            return dbType;
        }

        // Infer from connection string
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ?? string.Empty;
        if (connectionString.StartsWith("Server=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Initial Catalog", StringComparison.OrdinalIgnoreCase))
        {
            return DatabaseType.SqlServer;
        }

        // Default to PostgreSQL
        return DatabaseType.PostgreSQL;
    }
}

/// <summary>
/// FluentValidation validator for SubscrioConfig
/// Equivalent to TypeScript Zod schema validation
/// </summary>
public class SubscrioConfigValidator : AbstractValidator<SubscrioConfig>
{
    public SubscrioConfigValidator()
    {
        RuleFor(x => x.Database)
            .NotNull()
            .WithMessage("Database configuration is required");

        RuleFor(x => x.Database.ConnectionString)
            .NotEmpty()
            .WithMessage("Database connection string is required");

        RuleFor(x => x.Database.PoolSize)
            .InclusiveBetween(1, 100)
            .When(x => x.Database.PoolSize.HasValue)
            .WithMessage("Database pool size must be between 1 and 100");

        RuleFor(x => x.AdminPassphrase)
            .MinimumLength(8)
            .When(x => !string.IsNullOrEmpty(x.AdminPassphrase))
            .WithMessage("Admin passphrase must be at least 8 characters");

        RuleFor(x => x.Stripe)
            .SetValidator(new StripeConfigValidator())
            .When(x => x.Stripe != null);

        RuleFor(x => x.Logging)
            .SetValidator(new LoggingConfigValidator())
            .When(x => x.Logging != null);
    }
}

/// <summary>
/// FluentValidation validator for StripeConfig
/// </summary>
public class StripeConfigValidator : AbstractValidator<StripeConfig>
{
    public StripeConfigValidator()
    {
        RuleFor(x => x.SecretKey)
            .NotEmpty()
            .WithMessage("Stripe secret key is required")
            .Must(key => key.StartsWith("sk_", StringComparison.Ordinal))
            .WithMessage("Stripe secret key must start with 'sk_'");
    }
}

/// <summary>
/// FluentValidation validator for LoggingConfig
/// </summary>
public class LoggingConfigValidator : AbstractValidator<LoggingConfig>
{
    public LoggingConfigValidator()
    {
        RuleFor(x => x.Level)
            .IsInEnum()
            .WithMessage("Log level must be one of: Debug, Info, Warn, Error");
    }
}

