using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MIF.SharedKernel.Interfaces;

namespace MIF.SharedKernel.Extensions;

/// <summary>
/// Convenience helpers to discover and register module endpoints.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Scans the provided assemblies for <see cref="IEndpoint"/> implementations
    /// and registers them in the DI container.
    /// </summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly[] assemblies)
    {
        var endpointTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var endpointType in endpointTypes)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEndpoint), endpointType));
        }

        return services;
    }

    /// <summary>
    /// Resolves all registered endpoints and lets each one map its routes.
    /// This keeps endpoint registration centralized while allowing modules to own their routes.
    /// </summary>
    public static IApplicationBuilder MapEndpoints(this WebApplication app)
    {
        var endpoints = app.Services.CreateScope().ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>();
        
        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
