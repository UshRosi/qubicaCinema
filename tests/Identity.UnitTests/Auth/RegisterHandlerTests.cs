using Microsoft.AspNetCore.Identity;
using NSubstitute;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Identity.Api.Auth;
using QubicaCinema.Identity.Api.Exceptions;
using QubicaCinema.Identity.Persistence;
using QubicaCinema.Identity.UnitTests.Fixtures;

namespace QubicaCinema.Identity.UnitTests.Auth;

/// <summary>Registration makes a customer, and only ever a customer.</summary>
public sealed class RegisterHandlerTests
{
    private readonly UserManager<ApplicationUser> _users = TestUserManager.Substitute();

    public RegisterHandlerTests()
    {
        _users.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        _users.AddToRoleAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
    }

    [Fact]
    public async Task Should_create_the_user_with_the_details_given_and_the_Customer_role()
    {
        RegisteredUserResponse response = await Handle();

        response.Email.ShouldBe("ada@example.com");
        response.UserId.ShouldNotBe(Guid.Empty);
        await _users.Received(1).CreateAsync(
            Arg.Is<ApplicationUser>(user => user.Email == "ada@example.com" && user.FirstName == "Ada" && user.LastName == "Lovelace"),
            "Correct-horse-1");
        await _users.Received(1).AddToRoleAsync(Arg.Is<ApplicationUser>(user => user.Id == response.UserId), CinemaRoles.Customer);
    }

    [Fact]
    public async Task Should_never_hand_out_the_Admin_role()
    {
        await Handle();

        await _users.DidNotReceive().AddToRoleAsync(Arg.Any<ApplicationUser>(), CinemaRoles.Admin);
    }

    [Theory]
    [InlineData("DuplicateEmail")]
    [InlineData("DuplicateUserName")]
    public async Task Should_report_an_email_that_is_taken_as_a_conflict(string code)
    {
        _users.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(TestUserManager.Failed(code, "Email is already taken."));

        await Should.ThrowAsync<EmailAlreadyRegisteredException>(Handle());
        await _users.DidNotReceive().AddToRoleAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Should_pass_on_what_Identity_says_about_a_weak_password()
    {
        _users.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(TestUserManager.Failed("PasswordRequiresDigit", "Passwords must have at least one digit."));

        RegistrationRejectedException rejected = await Should.ThrowAsync<RegistrationRejectedException>(Handle());

        rejected.Extensions["errors"].ShouldBeAssignableTo<IReadOnlyCollection<string>>()!
            .ShouldBe(["Passwords must have at least one digit."]);
    }

    private Task<RegisteredUserResponse> Handle() =>
        new RegisterHandler(_users).HandleAsync(
            new RegisterCommand("ada@example.com", "Correct-horse-1", "Ada", "Lovelace"),
            TestContext.Current.CancellationToken);
}
