using Subscrio.Core;
using Subscrio.Core.Config;
using Subscrio.Core.Domain.ValueObjects;
using Xunit;

namespace Subscrio.Core.Tests.Setup;

/// <summary>
/// xUnit collection fixture for shared test database
/// </summary>
[CollectionDefinition("Database", DisableParallelization = true)]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
}

public class TestDatabaseFixture : IAsyncLifetime
{
    private static string? _sharedDbName;
    private static bool _databaseInitialized = false;

    public Subscrio Subscrio { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Only initialize database once (first fixture instance)
        if (!_databaseInitialized)
        {
            Console.WriteLine("🏗️  Setting up test database...");

            // Clean up any dangling test databases first
            await TestDatabase.CleanupDanglingTestDatabasesAsync();

            var context = await TestDatabase.SetupTestDatabaseAsync();
            _sharedDbName = context.DbName;
            _databaseInitialized = true;

            Console.WriteLine($"✅ Test database ready: {context.DbName}\n");
        }

        // Create a NEW Subscrio instance for this test class
        // Each test class gets its own Subscrio → its own DbContext → no tracking conflicts
        var connectionString = TestDatabase.GetTestConnectionString();
        var config = new SubscrioConfig
        {
            Database = new DatabaseConfig
            {
                ConnectionString = connectionString,
                Ssl = false,
                PoolSize = 10,
                DatabaseType = Domain.ValueObjects.DatabaseType.PostgreSQL
            }
        };

        Subscrio = new Subscrio(config);
    }

    public async Task DisposeAsync()
    {
        // Dispose this test class's Subscrio instance
        if (Subscrio != null)
        {
            Subscrio.Dispose();
        }

        // Only teardown database once (last fixture instance)
        if (_databaseInitialized && !string.IsNullOrEmpty(_sharedDbName))
        {
            Console.WriteLine("\n🧹 Tearing down test database...");
            await TestDatabase.TeardownTestDatabaseAsync(_sharedDbName);
            Console.WriteLine("✅ Test database cleaned up");
            _sharedDbName = null;
            _databaseInitialized = false;
        }
    }
}

