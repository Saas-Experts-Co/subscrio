using Microsoft.Extensions.DependencyInjection;
using Subscrio.Core.Config;

namespace Subscrio.Core.DependencyInjection;

/// <summary>
/// Extension methods for registering Subscrio with <see cref="IServiceCollection"/>.
/// </summary>
public static class SubscrioServiceCollectionExtensions
{
    /// <summary>
    /// Registers Subscrio in the service collection with the specified lifetime.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">Subscrio configuration (database, optional Stripe, etc.).</param>
    /// <param name="lifetime">Service lifetime. Use <see cref="ServiceLifetime.Scoped"/> for web apps.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSubscrio(
        this IServiceCollection services,
        SubscrioConfig config,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        var descriptor = ServiceDescriptor.Describe(
            typeof(Subscrio),
            _ => new Subscrio(config),
            lifetime);

        services.Add(descriptor);
        return services;
    }
}
