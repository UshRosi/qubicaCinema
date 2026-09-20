using Microsoft.AspNetCore.Identity;
using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.Identity.Api.Exceptions;
using QubicaCinema.Identity.Api.Tokens;
using QubicaCinema.Identity.Persistence;

namespace QubicaCinema.Identity.Api.Auth;

/// <summary>Checks the credentials and issues a token.</summary>
internal sealed class LoginHandler(UserManager<ApplicationUser> users, ITokenService tokens)
    : ICommandHandler<LoginCommand, AccessTokenResponse>
{
    /// <inheritdoc />
    /// <exception cref="InvalidCredentialsException">No such user, or the password is wrong: deliberately indistinguishable.</exception>
    public async Task<AccessTokenResponse> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await users.FindByEmailAsync(command.Email);

        // The same exception for both failures, so the response never says which one it was. The time taken
        // still differs (an unknown email skips the password hash); the gateway's rate limit is what bounds
        // how much that can be used to probe, and it is a known trade-off, not an oversight.
        if (user is null || !await users.CheckPasswordAsync(user, command.Password))
        {
            throw new InvalidCredentialsException();
        }

        // Loaded here and handed to the service, which stays pure: see ITokenService.
        IList<string> roles = await users.GetRolesAsync(user);
        AccessToken token = tokens.CreateAccessToken(user.Id, user.Email!, [.. roles]);

        return new AccessTokenResponse(token.Value, token.ExpiresAt, [.. roles]);
    }
}
