using FluentValidation;
using QubicaCinema.Identity.Persistence;

namespace QubicaCinema.Identity.Api.Auth;

/// <summary>Checks what can be decided from the body alone.</summary>
/// <remarks>
/// The composition rules of a password (digits, symbols) are Identity's and configurable, so they are not
/// repeated here: Identity reports them and <see cref="RegisterHandler"/> passes them on.
/// </remarks>
internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>The longest email address that exists, per RFC 5321.</summary>
    private const int MaxEmailLength = 254;

    private const int MaxNameLength = 100;

    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(MaxEmailLength);
        RuleFor(request => request.Password).NotEmpty().MinimumLength(PasswordPolicy.MinimumLength);
        RuleFor(request => request.FirstName).NotEmpty().MaximumLength(MaxNameLength);
        RuleFor(request => request.LastName).NotEmpty().MaximumLength(MaxNameLength);
    }
}
