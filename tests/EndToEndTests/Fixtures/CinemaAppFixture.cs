using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Hosting;

namespace QubicaCinema.EndToEndTests.Fixtures;

/// <summary>
/// The whole topology, booted once for the assembly: the real AppHost, a real SQL Server, a real RabbitMQ
/// and all four HTTP resources.
/// </summary>
/// <remarks>
/// Registered with <c>[assembly: AssemblyFixture&lt;CinemaAppFixture&gt;]</c>: built once, before any test
/// runs, torn down once, after the last one finishes. Configuration is set as process environment variables,
/// <em>before</em> <c>DistributedApplicationTestingBuilder.CreateAsync</c> is called —
/// the same lesson <c>tests/Services.IntegrationTests</c> learned the hard way: <c>src/AppHost/Program.cs</c>
/// reads <c>Cinema:*</c> into <c>AppHostOptions</c> as one of its very first statements, and there is no
/// guarantee that a <c>configureBuilder</c> delegate supplied to <c>CreateAsync</c> is merged in before that
/// line runs. An environment variable set on this process, by contrast, is visible the moment
/// <c>DistributedApplication.CreateBuilder</c> reads the environment — which is exactly how the AppHost
/// would receive these values under any other deployment too, just set here instead of by an orchestrator.
/// In fact every one of these already matches <c>src/AppHost/appsettings.json</c>'s own defaults; they are
/// still set explicitly so a future change to that file cannot silently make this run non-reproducible.
/// </remarks>
public sealed class CinemaAppFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RoutingTimeout = TimeSpan.FromSeconds(30);

    private DistributedApplication _app = null!;

    /// <summary>The gateway — the only resource a client outside the orchestrator is meant to reach.</summary>
    public HttpClient Gateway { get; private set; } = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", Environments.Development);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", Environments.Development);
        Environment.SetEnvironmentVariable("Cinema__UseVolumes", "false");
        Environment.SetEnvironmentVariable("Cinema__PersistentContainers", "false");
        Environment.SetEnvironmentVariable("Cinema__SeedData", "true");

        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
            [],
            (_, settings) => settings.EnvironmentName = Environments.Development,
            cancellationToken);

        _app = await builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        using var startupTimeout = new CancellationTokenSource(StartupTimeout);
        using var startupToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, startupTimeout.Token);

        // WaitForCompletion in the AppHost means every service already waits on this; waiting on it here
        // too turns a migration failure into a clear timeout message instead of a mysterious one from
        // whichever service's health check never turns green.
        await _app.ResourceNotifications.WaitForResourceAsync("migrations", KnownResourceStates.Finished, startupToken.Token);

        foreach (string resource in new[] { "identity", "catalog", "booking", "gateway" })
        {
            await _app.ResourceNotifications.WaitForResourceHealthyAsync(resource, startupToken.Token);
        }

        Gateway = _app.CreateHttpClient("gateway", "http");

        // The gateway has no WaitFor on the services it fronts — that independence is deliberate — so there
        // is a real window, even after every resource reports healthy, where YARP's active probe (a
        // 10-second interval) has not yet marked a destination healthy. A route in that window answers 503,
        // the same honest answer a client would see in production; this waits it out rather than racing it.
        await Eventually.UntilAsync(
            async () =>
            {
                using HttpResponseMessage response = await Gateway.GetAsync("/api/v1/movies", cancellationToken);

                return response.StatusCode != HttpStatusCode.ServiceUnavailable;
            },
            because: "the gateway's routes to become healthy",
            RoutingTimeout);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Gateway?.Dispose();
        await _app.DisposeAsync();
    }
}
