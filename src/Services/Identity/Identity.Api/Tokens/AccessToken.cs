namespace QubicaCinema.Identity.Api.Tokens;

/// <summary>A signed token and the moment it stops being valid.</summary>
/// <param name="Value">The compact JWT, ready for an <c>Authorization: Bearer</c> header.</param>
/// <param name="ExpiresAt">When validators start rejecting it.</param>
internal sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
