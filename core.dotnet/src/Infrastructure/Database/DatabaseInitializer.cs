using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subscrio.Core.Config;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Infrastructure.Database;

public static class DatabaseInitializer
{
    public static SubscrioDbContext InitializeDatabase(DatabaseConfig config)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SubscrioDbContext>();

        switch (config.DatabaseType)
        {
            case DatabaseType.PostgreSQL:
                optionsBuilder.UseNpgsql(config.ConnectionString, options =>
                {
                    if (config.Ssl)
                    {
                        options.EnableRetryOnFailure();
                    }
                });
                break;

            case DatabaseType.SqlServer:
                optionsBuilder.UseSqlServer(config.ConnectionString, options =>
                {
                    options.EnableRetryOnFailure();
                });
                break;

            default:
                throw new ArgumentException($"Unsupported database type: {config.DatabaseType}");
        }

        return new SubscrioDbContext(optionsBuilder.Options);
    }
}

