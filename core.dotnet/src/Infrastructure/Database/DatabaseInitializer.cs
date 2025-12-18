using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Subscrio.Core.Config;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Infrastructure.Database;

public class DatabaseInitializerResult
{
    public required SubscrioDbContext DbContext { get; init; }
    public NpgsqlDataSource? DataSource { get; init; }
}

public static class DatabaseInitializer
{
    public static DatabaseInitializerResult InitializeDatabase(DatabaseConfig config)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SubscrioDbContext>();
        NpgsqlDataSource? dataSource = null;

        switch (config.DatabaseType)
        {
            case DatabaseType.PostgreSQL:
                // Create NpgsqlDataSource with dynamic JSON enabled for Dictionary<string, object?> serialization
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(config.ConnectionString);
                dataSourceBuilder.EnableDynamicJson();
                dataSource = dataSourceBuilder.Build();
                
                optionsBuilder.UseNpgsql(dataSource, options =>
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

        return new DatabaseInitializerResult
        {
            DbContext = new SubscrioDbContext(optionsBuilder.Options),
            DataSource = dataSource
        };
    }
}

