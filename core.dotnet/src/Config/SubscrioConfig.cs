using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Config;

/// <summary>
/// Subscrio configuration
/// Equivalent to TypeScript config/types.ts SubscrioConfig interface
/// </summary>
public class SubscrioConfig
{
    public required DatabaseConfig Database { get; init; }
    public string? AdminPassphrase { get; init; }
    public StripeConfig? Stripe { get; init; }
    public LoggingConfig? Logging { get; init; }
}

/// <summary>
/// Database configuration
/// </summary>
public class DatabaseConfig
{
    public required string ConnectionString { get; init; }
    public bool? Ssl { get; init; }
    public int? PoolSize { get; init; }
    public DatabaseType DatabaseType { get; init; } = DatabaseType.PostgreSQL;
}

/// <summary>
/// Stripe configuration
/// </summary>
public class StripeConfig
{
    public required string SecretKey { get; init; }
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfig
{
    public LogLevel Level { get; init; } = LogLevel.Info;
}

/// <summary>
/// Log level enumeration
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}

