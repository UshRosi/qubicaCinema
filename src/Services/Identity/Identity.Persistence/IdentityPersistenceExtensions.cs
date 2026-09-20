using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QubicaCinema.BuildingBlocks.Persistence;

namespace QubicaCinema.Identity.Persistence;

/// <summary>Registers ASP.NET Core Identity over the Identity database.</summary>
public static class IdentityPersistenceExtensions
{
    /// <summary>The name of the connection string, and of the health check that watches it.</summary>
    public const string DatabaseName = "identitydb";

    /// <summary>Wires up the user and role stores against the given connection string.</summary>
    /// <remarks>
    /// The connection string is a parameter, as in the other services. <c>AddIdentityCore</c> and not
    /// <c>AddIdentity</c>: this registers the managers and the stores and nothing about cookies or sign-in
    /// pages, because the only thing this service hands out is a token.
    /// </remarks>
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CinemaIdentityDbContext>(options =>
            // A containerised SQL Server drops connections while it starts; see Catalog for the same reason.
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Two accounts on one address would make "log in with your email" ambiguous.
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = PasswordPolicy.MinimumLength;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<CinemaIdentityDbContext>();

        // Readiness, not liveness: an unreachable database means it cannot serve, not that it should be restarted.
        services.AddHealthChecks()
            .AddDbContextCheck<CinemaIdentityDbContext>(DatabaseName, tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Adds the initializer the migration service runs: the schema, the roles and the seeded administrator.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="AddIdentityPersistence"/> because only the migration service seeds. The API
    /// must start without an administrator's password in its configuration, and it would be wrong for it to
    /// need one.
    /// </remarks>
    public static IServiceCollection AddIdentityDatabaseInitializer(
        this IServiceCollection services,
        IConfigurationSection seedSection)
    {
        services.AddOptions<IdentitySeedOptions>()
            .Bind(seedSection)
            .ValidateDataAnnotations()
            // A missing administrator password should stop the migration, not surface as a login that never works.
            .ValidateOnStart();
        services.AddScoped<IDatabaseInitializer, IdentityDatabaseInitializer>();

        return services;
    }
}
