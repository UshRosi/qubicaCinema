using Microsoft.AspNetCore.Identity;
using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Identity.Api.Exceptions;
using QubicaCinema.Identity.Persistence;

namespace QubicaCinema.Identity.Api.Auth;

/// <summary>Registers a customer. Nobody can register as an administrator: that account is seeded.</summary>
internal sealed class RegisterHandler(UserManager<ApplicationUser> users)
    : ICommandHandler<RegisterCommand, RegisteredUserResponse>
{
    /// <inheritdoc />
    /// <exception cref="EmailAlreadyRegisteredException">An account exists for this email.</exception>
    /// <exception cref="RegistrationRejectedException">Identity refused the password.</exception>
    public async Task<RegisteredUserResponse> HandleAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        var user = ApplicationUser.Register(command.Email, command.FirstName, command.LastName);

        IdentityResult created = await users.CreateAsync(user, command.Password);

        if (!created.Succeeded)
        {
            throw Reject(created);
        }

        // Two writes, so a failure between them leaves a user with no role. A customer who then cannot book is
        // a support ticket; the seed heals its own equivalent, and here the next registration attempt reports
        // the email as taken. Acceptable for this scope, and the honest fix is a transaction around both.
        IdentityResult withRole = await users.AddToRoleAsync(user, CinemaRoles.Customer);

        if (!withRole.Succeeded)
        {
            throw Reject(withRole);
        }

        return new RegisteredUserResponse(user.Id, user.Email!);
    }

    private static Exception Reject(IdentityResult result) =>
        result.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail)
                                   or nameof(IdentityErrorDescriber.DuplicateUserName))
            ? new EmailAlreadyRegisteredException()
            : new RegistrationRejectedException([.. result.Errors.Select(error => error.Description)]);
}
