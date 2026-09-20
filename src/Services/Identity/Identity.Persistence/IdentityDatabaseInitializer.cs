using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.BuildingBlocks.Persistence;

namespace QubicaCinema.Identity.Persistence;

/// <summary>Creates the Identity schema, the two roles and the seeded administrator.</summary>
internal sealed class IdentityDatabaseInitializer(
    CinemaIdentityDbContext context,
    RoleManager<ApplicationRole> roles,
    UserManager<ApplicationUser> users,
    IOptions<IdentitySeedOptions> seed,
    ILogger<IdentityDatabaseInitializer> logger) : IDatabaseInitializer
{
    /// <inheritdoc />
    public string DatabaseName => IdentityPersistenceExtensions.DatabaseName;

    /// <inheritdoc />
    /// <remarks>
    /// The roles are created here, and not in <see cref="SeedAsync"/>, because they are not demo data: with
    /// seeding switched off, registration would still need the Customer role to exist. Creating them through
    /// <c>RoleManager</c> rather than a migration's <c>HasData</c> keeps the name normalisation the framework
    /// looks roles up by.
    /// </remarks>
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await context.Database.MigrateAsync(cancellationToken);

        foreach (string role in new[] { CinemaRoles.Admin, CinemaRoles.Customer })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                Require(await roles.CreateAsync(ApplicationRole.Named(role)), $"create the {role} role");
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Goes through <c>UserManager</c>, which hashes the password and normalises the email exactly as
    /// registration does; a row inserted with SQL would carry a hash nothing could verify. It checks the user
    /// and the role separately, so a run that stopped between the two heals on the next start.
    /// </remarks>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        IdentitySeedOptions options = seed.Value;

        ApplicationUser? administrator = await users.FindByEmailAsync(options.AdministratorEmail);

        if (administrator is null)
        {
            administrator = ApplicationUser.Register(
                options.AdministratorEmail, options.AdministratorFirstName, options.AdministratorLastName);
            Require(await users.CreateAsync(administrator, options.AdministratorPassword), "create the administrator");
        }

        if (await users.IsInRoleAsync(administrator, CinemaRoles.Admin))
        {
            logger.LogInformation("{Database} already has its administrator; leaving it alone.", DatabaseName);
            return;
        }

        Require(await users.AddToRoleAsync(administrator, CinemaRoles.Admin), "give the administrator the Admin role");
        logger.LogInformation("Seeded {Database} with the administrator {Email}.", DatabaseName, options.AdministratorEmail);
    }

    private static void Require(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not {action}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}
