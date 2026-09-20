namespace QubicaCinema.Identity.Api.Auth;

/// <summary>What a successful login returns.</summary>
/// <param name="AccessToken">The signed token, to send as <c>Authorization: Bearer</c>.</param>
/// <param name="ExpiresAt">When it stops working.</param>
/// <param name="Roles">The roles it carries, for a client that wants to adapt its UI without decoding it.</param>
public sealed record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt, IReadOnlyCollection<string> Roles);
