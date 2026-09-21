using Microsoft.AspNetCore.Http;
using QubicaCinema.BuildingBlocks.Application.Security;

namespace QubicaCinema.BuildingBlocks.Authentication;

/// <summary>The caller, read from the principal that bearer validation produced.</summary>
/// <remarks>
/// The only place that touches <c>HttpContext</c> on behalf of a use case. Application asks
/// <see cref="ICurrentUser"/> who is calling and never sees a claim or a header.
/// </remarks>
internal sealed class ClaimsPrincipalCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">There is no authenticated caller: the route forgot to require one.</exception>
    public Guid UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(CinemaClaimTypes.Subject)?.Value, out Guid id)
            ? id
            // Failing loudly is the point. Falling back to Guid.Empty would let an unprotected route create a
            // booking that belongs to nobody, and nothing would say why.
            : throw new InvalidOperationException(
                "There is no authenticated user. The route must require authorization before it reads the current user.");

    /// <inheritdoc />
    public bool IsAdministrator =>
        httpContextAccessor.HttpContext?.User.IsInRole(CinemaRoles.Admin) ?? false;
}
