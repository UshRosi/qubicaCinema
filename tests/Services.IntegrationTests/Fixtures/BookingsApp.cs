extern alias bookings;

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using QubicaCinema.BuildingBlocks.Persistence;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>The real Booking service, hosted in memory, against the shared containers.</summary>
/// <remarks>
/// Booking has no outbox of its own — it only consumes Catalog's events — so unlike
/// <see cref="CatalogApp"/> there is no publisher to switch off. Its consumer,
/// <c>RabbitMqConsumerService</c>, is a genuine hosted service and is left running: it only starts declaring
/// the <c>booking</c> queue and its bindings once this host's <see cref="WebApplicationFactory{TEntryPoint}.Services"/>
/// is first touched, which is why <see cref="RabbitMqFixture.WaitForBookingBindingsAsync"/> exists.
/// Configuration comes from the process environment, set once by <see cref="CinemaFixture"/> — see the
/// remark there for why <c>ConfigureAppConfiguration</c> cannot reach <c>Program.cs</c>'s connection strings
/// in time.
/// </remarks>
internal sealed class BookingsApp(FakeTimeProvider clock) : WebApplicationFactory<bookings::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Runs after Program.cs, so this wins over both AddBookingsApplication's and
            // AddBookingsInfrastructure's TryAddSingleton(TimeProvider.System).
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
        });
    }

    /// <summary>Applies Booking's migrations against the shared server.</summary>
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
