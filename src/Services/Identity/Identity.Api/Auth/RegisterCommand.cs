namespace QubicaCinema.Identity.Api.Auth;

/// <summary>Create an account for a customer.</summary>
internal sealed record RegisterCommand(string Email, string Password, string FirstName, string LastName);
