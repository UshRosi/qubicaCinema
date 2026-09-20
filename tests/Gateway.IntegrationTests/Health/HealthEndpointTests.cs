using System.Net;
using QubicaCinema.Gateway.IntegrationTests.Fixtures;

namespace QubicaCinema.Gateway.IntegrationTests.Health;

/// <summary>The gateway answers for itself on /health and /alive, and never proxies them.</summary>
public sealed class HealthEndpointTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task The_gateways_own_health_is_answered_locally_and_never_forwarded(string path)
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Downstream.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Health_endpoints_are_exempt_from_the_rate_limiter(string path)
    {
        await using var factory = new GatewayFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:PermitLimit"] = "3",
            ["RateLimiting:BookingCreationPermitLimit"] = "1",
        });
        using var client = factory.CreateClient();

        List<HttpStatusCode> statuses = [];
        for (int attempt = 0; attempt < 10; attempt++)
        {
            using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
            statuses.Add(response.StatusCode);
        }

        // A load balancer polls these far more often than any client calls the API; a 429 here would take
        // the gateway out of rotation for being busy.
        statuses.ShouldAllBe(status => status == HttpStatusCode.OK);
    }
}
