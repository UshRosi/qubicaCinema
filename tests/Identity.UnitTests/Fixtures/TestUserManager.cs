using Microsoft.AspNetCore.Identity;
using QubicaCinema.Identity.Persistence;

namespace QubicaCinema.Identity.UnitTests.Fixtures;

/// <summary>A <see cref="UserManager{TUser}"/> whose every call the test can script.</summary>
internal static class TestUserManager
{
    /// <summary>
    /// UserManager is a class with a nine-argument constructor and virtual methods, which is what makes it
    /// substitutable. The collaborators it would use for real (hashing, validation) are irrelevant to the
    /// handlers under test, so they are null.
    /// </summary>
    internal static UserManager<ApplicationUser> Substitute() =>
        NSubstitute.Substitute.For<UserManager<ApplicationUser>>(
            NSubstitute.Substitute.For<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

    internal static IdentityResult Failed(string code, string description) =>
        IdentityResult.Failed(new IdentityError { Code = code, Description = description });
}
