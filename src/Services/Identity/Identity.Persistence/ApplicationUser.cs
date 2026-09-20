using Microsoft.AspNetCore.Identity;

namespace QubicaCinema.Identity.Persistence;

/// <summary>A person who can sign in.</summary>
/// <remarks>
/// Everything Identity itself owns (the email, the password hash, the lockout counters) comes from the base
/// type and is managed through <c>UserManager</c>. This adds the two facts the cinema wants, behind a factory
/// so a user is never half-built.
/// </remarks>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    // For EF Core, which materialises entities without going through the factory.
    private ApplicationUser()
    {
    }

    /// <summary>The user's given name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>The user's family name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Creates a user who is about to register. The email doubles as the user name.</summary>
    /// <param name="email">The address the user signs in with.</param>
    /// <param name="firstName">The given name.</param>
    /// <param name="lastName">The family name.</param>
    public static ApplicationUser Register(string email, string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        return new ApplicationUser
        {
            // Version 7, like every other id in the solution: time-ordered, so the clustered key stays append-only.
            Id = Guid.CreateVersion7(),
            UserName = email.Trim(),
            Email = email.Trim(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
        };
    }
}
