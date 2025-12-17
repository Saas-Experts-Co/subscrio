using Subscrio.Core.Config;

namespace Subscrio.Sample;

public static class SampleConfig
{
    public static SubscrioConfig LoadConfig()
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.Error.WriteLine("ERROR: DATABASE_URL environment variable is required");
            Console.Error.WriteLine("Please set DATABASE_URL in your environment or create a .env file");
            Console.Error.WriteLine("Example: DATABASE_URL=Host=localhost;Port=5432;Database=subscrio_demo;Username=postgres;Password=postgres");
            Environment.Exit(1);
        }

        return ConfigLoader.LoadConfig();
    }
}


