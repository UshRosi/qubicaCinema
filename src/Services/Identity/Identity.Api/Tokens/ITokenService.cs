namespace QubicaCinema.Identity.Api.Tokens;

/// <summary>Turns "this user, with these roles" into a signed token.</summary>
/// <remarks>
/// Pure on purpose: everything it needs is a parameter, so it never loads a user or their roles. That is what
/// lets it be a singleton. An implementation that reached for <c>UserManager</c> would have to be scoped, and a
/// singleton that captured it would be exactly the captive dependency the container is set to reject.
/// </remarks>
internal interface ITokenService
{
    /// <summary>Signs an access token for the given user.</summary>
    AccessToken CreateAccessToken(Guid userId, string email, IReadOnlyCollection<string> roles);
}
