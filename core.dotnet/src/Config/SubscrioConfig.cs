using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Config;

public class SubscrioConfig
{
    public required DatabaseConfig Database { get; init; }
    public string? AdminPassphrase { get; init; }
    public StripeConfig? Stripe { get; init; }
    public LoggingConfig? Logging { get; init; }
}

public class DatabaseConfig
{
    public required string ConnectionString { get; init; }
    public bool Ssl { get; init; }
    public int PoolSize { get; init; } = 10;
    public DatabaseType DatabaseType { get; init; } = DatabaseType.PostgreSQL;
}

public class StripeConfig
{
    public required string SecretKey { get; init; }
}

public class LoggingConfig
{
    public LogLevel Level { get; init; } = LogLevel.Info;
}

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}

