using Microsoft.AspNetCore.Routing;

namespace MIF.SharedKernel.Interfaces;

/// <summary>
/// Contract for minimal API endpoint classes.
/// Implementations encapsulate route mapping for a feature/module.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Adds routes to the provided endpoint builder.
    /// </summary>
    void MapEndpoint(IEndpointRouteBuilder app);
}
