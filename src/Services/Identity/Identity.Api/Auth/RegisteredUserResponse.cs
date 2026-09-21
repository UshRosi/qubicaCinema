namespace QubicaCinema.Identity.Api.Auth;

/// <summary>The account that was created.</summary>
/// <param name="UserId">The id the user's bookings will carry.</param>
/// <param name="Email">The address they registered with.</param>
public sealed record RegisteredUserResponse(Guid UserId, string Email);
