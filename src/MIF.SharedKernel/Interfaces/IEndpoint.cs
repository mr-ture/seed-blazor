using Microsoft.AspNetCore.Routing;

namespace MIF.SharedKernel.Interfaces;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
