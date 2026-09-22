extern alias identity;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using QubicaCinema.BuildingBlocks.Persistence;
using QubicaCinema.Identity.Persistence;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>The real Identity service, hosted in memory, against the shared SQL Server container.</summary>
/// <remarks>
/// Two things this factory does that its siblings do not. First, it keeps the real system clock:
/// <c>Identity.Api</c>'s only <see cref="TimeProvider"/> consumer is the token service, and bearer
/// validation everywhere else in this solution checks a token's lifetime against
/// <see cref="DateTime.UtcNow"/> with no <see cref="TimeProvider"/> seam — freezing this host's clock would
/// make every token it issues look already expired or not yet valid the moment another host validated it.
/// Second, <c>Identity.Api/Program.cs</c> registers <see cref="IdentityPersistenceExtensions.AddIdentityPersistence"/>
/// only: <see cref="IdentityPersistenceExtensions.AddIdentityDatabaseInitializer"/>, which creates the
/// <c>Admin</c> and <c>Customer</c> roles and is normally called by the migration service alone, is added
/// here — without it <c>identitydb</c> is never created, and every registration would fail because
/// <c>RegisterHandler</c> adds the new user to a <c>Customer</c> role that does not exist. The seed values
/// are a standalone configuration object, never read from <c>IConfiguration</c>, so it needs none of the
/// process-environment plumbing <see cref="CinemaFixture"/> sets up for the connection strings.
/// </remarks>
internal sealed class IdentityApp : WebApplicationFactory<identity::Program>
{
    private static readonly IConfigurationSection SeedSection = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{IdentitySeedOptions.SectionName}:AdministratorEmail"] = "admin@services.tests",
            [$"{IdentitySeedOptions.SectionName}:AdministratorPassword"] = "Cinema!Test1",
        })
        .Build()
        .GetSection(IdentitySeedOptions.SectionName);

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services => services.AddIdentityDatabaseInitializer(SeedSection));

    /// <summary>Applies Identity's migrations and creates its two roles. Does not seed the administrator.</summary>
    internal async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        foreach (IDatabaseInitializer initializer in scope.ServiceProvider.GetServices<IDatabaseInitializer>())
        {
            await initializer.MigrateAsync(cancellationToken);
        }
    }
}
