using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yarp.ReverseProxy.Forwarder;

namespace QubicaCinema.Gateway.IntegrationTests.Fixtures;

/// <summary>
/// The real gateway, hosted in memory, with both services replaced by <see cref="Downstream"/>.
/// </summary>
internal sealed class GatewayFactory(IReadOnlyDictionary<string, string?>? settings = null)
    : WebApplicationFactory<Program>
{
    /// <summary>The host the recorder sees for a request routed to Catalog.</summary>
    internal const string CatalogHost = "catalog.test";

    /// <summary>The host the recorder sees for a request routed to Booking.</summary>
    internal const string BookingHost = "booking.test";

    /// <summary>Records what the gateway forwards and answers on behalf of both services.</summary>
    internal RecordingHandler Downstream { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            Dictionary<string, string?> values = new()
            {
                // Distinct hosts, so the recorder can tell the two services apart.
                ["ReverseProxy:Clusters:catalog:Destinations:primary:Address"] = $"http://{CatalogHost}/",
                ["ReverseProxy:Clusters:booking:Destinations:primary:Address"] = $"http://{BookingHost}/",

                // The active probe would also go through the recorder and add noise to every assertion. It is
                // YARP's own code; what is under test here is the routing table and the middleware order.
                ["ReverseProxy:Clusters:catalog:HealthCheck:Active:Enabled"] = "false",
                ["ReverseProxy:Clusters:booking:HealthCheck:Active:Enabled"] = "false",

                // Mapped in Development anyway; stated here so the tests do not depend on the environment.
                ["HealthChecks:Expose"] = "true",
            };

            foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            {
                values[key] = value;
            }

            configuration.AddInMemoryCollection(values);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IForwarderHttpClientFactory>();
            services.AddSingleton<IForwarderHttpClientFactory>(new RecordingForwarderHttpClientFactory(Downstream));
        });
    }
}
