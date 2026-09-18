using Microsoft.AspNetCore.Routing;

namespace QubicaCinema.BuildingBlocks.Api.Endpoints;

/// <summary>Maps endpoint modules onto a route builder.</summary>
public static class EndpointModuleExtensions
{
    /// <summary>Maps one module's routes. Reads as a list of features in <c>Program.cs</c>.</summary>
    public static IEndpointRouteBuilder MapModule<TModule>(this IEndpointRouteBuilder endpoints)
        where TModule : IEndpointModule
    {
        TModule.Map(endpoints);

        return endpoints;
    }
}
