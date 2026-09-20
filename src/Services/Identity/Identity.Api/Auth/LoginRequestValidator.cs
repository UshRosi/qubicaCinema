using FluentValidation;

namespace QubicaCinema.Identity.Api.Auth;

/// <summary>
/// Checks only that both fields are present. Nothing about their shape: a login has to answer 401 for a
/// wrong password, and a rule here that told a caller their password is "too short" would say what the rules are.
/// </summary>
internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty();
        RuleFor(request => request.Password).NotEmpty();
    }
}
