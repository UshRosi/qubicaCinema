using QubicaCinema.Identity.Api.Auth;
using QubicaCinema.Identity.Persistence;

namespace QubicaCinema.Identity.UnitTests.Auth;

/// <summary>What can be decided from the body alone, before Identity is asked anything.</summary>
public sealed class RequestValidatorTests
{
    private static readonly RegisterRequestValidator Register = new();
    private static readonly LoginRequestValidator Login = new();

    [Fact]
    public void Should_accept_a_complete_registration()
    {
        Register.Validate(new RegisterRequest("ada@example.com", "Correct-horse-1", "Ada", "Lovelace")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "Correct-horse-1", "Ada", "Lovelace")]
    [InlineData("not-an-email", "Correct-horse-1", "Ada", "Lovelace")]
    [InlineData("ada@example.com", "", "Ada", "Lovelace")]
    [InlineData("ada@example.com", "short", "Ada", "Lovelace")]
    [InlineData("ada@example.com", "Correct-horse-1", "", "Lovelace")]
    [InlineData("ada@example.com", "Correct-horse-1", "Ada", "")]
    public void Should_reject_a_registration_with_a_missing_or_malformed_field(
        string email, string password, string firstName, string lastName)
    {
        Register.Validate(new RegisterRequest(email, password, firstName, lastName)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Should_require_the_password_length_Identity_requires()
    {
        string oneShort = new('x', PasswordPolicy.MinimumLength - 1);
        string enough = new('x', PasswordPolicy.MinimumLength);

        Register.Validate(new RegisterRequest("ada@example.com", oneShort, "Ada", "Lovelace")).IsValid.ShouldBeFalse();
        Register.Validate(new RegisterRequest("ada@example.com", enough, "Ada", "Lovelace")).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_not_tell_a_login_its_password_is_too_short()
    {
        // A short password on login is simply wrong, and the answer to wrong is the same 401 as ever.
        Login.Validate(new LoginRequest("ada@example.com", "x")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "x")]
    [InlineData("ada@example.com", "")]
    public void Should_require_both_fields_to_log_in(string email, string password)
    {
        Login.Validate(new LoginRequest(email, password)).IsValid.ShouldBeFalse();
    }
}
