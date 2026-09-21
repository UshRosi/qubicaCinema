using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Identity.Api.Auth;
using QubicaCinema.Identity.Api.Exceptions;
using QubicaCinema.Identity.Persistence;
using QubicaCinema.Identity.UnitTests.Fixtures;

namespace QubicaCinema.Identity.UnitTests.Auth;

/// <summary>Signing in, and above all that a failure never says which half of the credentials was wrong.</summary>
public sealed class LoginHandlerTests
{
    private const string Email = "ada@example.com";
    private const string Password = "Correct-horse-1";

    private readonly UserManager<ApplicationUser> _users = TestUserManager.Substitute();
    private readonly ApplicationUser _ada = ApplicationUser.Register(Email, "Ada", "Lovelace");

    public LoginHandlerTests()
    {
        _users.FindByEmailAsync(Email).Returns(_ada);
        _users.CheckPasswordAsync(_ada, Password).Returns(true);
        _users.GetRolesAsync(_ada).Returns([CinemaRoles.Customer]);
    }

    [Fact]
    public async Task Should_issue_a_token_for_the_user_with_their_roles()
    {
        AccessTokenResponse response = await Handle(Email, Password);

        response.Roles.ShouldBe([CinemaRoles.Customer]);
        response.ExpiresAt.ShouldBe(TestJwt.Start.AddMinutes(60));
        new JsonWebToken(response.AccessToken).Subject.ShouldBe(_ada.Id.ToString());
    }

    [Fact]
    public async Task Should_answer_a_wrong_password_and_an_unknown_email_identically()
    {
        InvalidCredentialsException wrongPassword =
            await Should.ThrowAsync<InvalidCredentialsException>(Handle(Email, "Wrong-password-1"));
        InvalidCredentialsException unknownEmail =
            await Should.ThrowAsync<InvalidCredentialsException>(Handle("nobody@example.com", Password));

        // The whole point: nothing in the response tells a caller which addresses have an account.
        unknownEmail.Message.ShouldBe(wrongPassword.Message);
        unknownEmail.Kind.ShouldBe(wrongPassword.Kind);
        wrongPassword.Kind.ShouldBe(QubicaCinema.BuildingBlocks.Domain.DomainErrorKind.Unauthenticated);
    }

    [Fact]
    public async Task Should_not_load_roles_or_sign_anything_when_the_password_is_wrong()
    {
        await Should.ThrowAsync<InvalidCredentialsException>(Handle(Email, "Wrong-password-1"));

        await _users.DidNotReceive().GetRolesAsync(Arg.Any<ApplicationUser>());
    }

    private Task<AccessTokenResponse> Handle(string email, string password) =>
        new LoginHandler(_users, TestJwt.TokenService())
            .HandleAsync(new LoginCommand(email, password), TestContext.Current.CancellationToken);
}
