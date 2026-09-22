extern alias catalog;

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using QubicaCinema.BuildingBlocks.Persistence;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>The real Catalog service, hosted in memory, against the shared containers.</summary>
/// <remarks>
/// <c>Program</c> is ambiguous across the three aliased Api assemblies, so <c>WebApplicationFactory</c> is
/// pointed at <c>catalog::Program</c> instead — the alias is needed only here and in the two sibling
/// factories; every test reaches this host over HTTP and never needs a type from the Api assembly itself.
/// Configuration comes from the process environment, set once by <see cref="CinemaFixture"/> before this
/// host is first touched — see the remark there for why <c>ConfigureAppConfiguration</c> cannot reach
/// <c>Program.cs</c>'s connection string and its outbox-publisher flag in time.
/// </remarks>
internal sealed class CatalogApp(FakeTimeProvider clock) : WebApplicationFactory<catalog::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Runs after Program.cs, so this wins over both AddCatalogApplication's and
            // AddCatalogInfrastructure's TryAddSingleton(TimeProvider.System).
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
        });
    }

    /// <summary>Applies Catalog's migrations against the shared server. Does not seed: the demo data is clock-relative.</summary>
    internal async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        foreach (IDatabaseInitializer initializer in scope.ServiceProvider.GetServices<IDatabaseInitializer>())
        {
            await initializer.MigrateAsync(cancellationToken);
        }
    }

    /// <summary>A client presenting a valid token for the user, holding the given roles.</summary>
    internal HttpClient CreateClientFor(Guid userId, params string[] roles)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.For(userId, roles));

        return client;
    }
}
