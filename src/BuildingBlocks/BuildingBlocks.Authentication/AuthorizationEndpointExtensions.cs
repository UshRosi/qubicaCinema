using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace QubicaCinema.BuildingBlocks.Authentication;

/// <summary>Protects a route and documents the two answers protection adds.</summary>
public static class AuthorizationEndpointExtensions
{
    /// <summary>
    /// Requires the named policy, and records the 401 and 403 in the route's metadata.
    /// </summary>
    /// <remarks>
    /// One call in place of three, so a route cannot be protected and left undocumented, or the other way
    /// round. The names are <see cref="CinemaPolicies"/>.
    /// </remarks>
    public static RouteHandlerBuilder RequiringPolicy(this RouteHandlerBuilder builder, string policy) =>
        builder
            .RequireAuthorization(policy)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
}
