namespace QubicaCinema.Identity.Api.Auth;

/// <summary>The body of <c>POST /auth/login</c>.</summary>
/// <param name="Email">The address the user registered with.</param>
/// <param name="Password">The user's password.</param>
public sealed record LoginRequest(string Email, string Password);
