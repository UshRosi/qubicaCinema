using Microsoft.AspNetCore.Identity;

namespace QubicaCinema.Identity.Persistence;

/// <summary>A role a user can hold: see <c>CinemaRoles</c> for the two that exist.</summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    // For EF Core.
    private ApplicationRole()
    {
    }

    /// <summary>Creates a role with the given name.</summary>
    public static ApplicationRole Named(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ApplicationRole { Id = Guid.CreateVersion7(), Name = name };
    }
}
