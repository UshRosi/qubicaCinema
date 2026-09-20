namespace QubicaCinema.Identity.Api.Auth;

/// <summary>The body of <c>POST /auth/register</c>.</summary>
/// <param name="Email">The address to sign in with.</param>
/// <param name="Password">The password. Never stored: only its hash is.</param>
/// <param name="FirstName">The given name.</param>
/// <param name="LastName">The family name.</param>
public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName);
