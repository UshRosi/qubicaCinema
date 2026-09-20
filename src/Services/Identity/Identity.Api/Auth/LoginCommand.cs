namespace QubicaCinema.Identity.Api.Auth;

/// <summary>Exchange an email and a password for an access token.</summary>
internal sealed record LoginCommand(string Email, string Password);
