using Microsoft.AspNetCore.Routing;

namespace QubicaCinema.BuildingBlocks.Api.Endpoints;

/// <summary>
/// One feature's HTTP surface: every route for movies, or for screenings, in one place.
/// </summary>
/// <remarks>
/// <c>static abstract</c> rather than an instance method, because a module has no state and nothing to
/// inject — mapping routes is a compile-time fact about a feature, not a service. It also means modules
/// are named explicitly in <c>Program.cs</c>: no assembly scanning, so the set of routes a process serves
/// can be read off one screen and a module cannot appear by accident.
/// </remarks>
public interface IEndpointModule
{
    /// <summary>Maps every route the feature owns onto the given builder.</summary>
    static abstract void Map(IEndpointRouteBuilder endpoints);
}
