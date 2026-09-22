using System.Net;
using QubicaCinema.BuildingBlocks.Api.OpenApi;
using QubicaCinema.Gateway.IntegrationTests.Fixtures;

namespace QubicaCinema.Gateway.IntegrationTests.Documentation;

/// <summary>
/// The reference page is the gateway's own, and the documents it lists are the services' — proxied, not built
/// here.
/// </summary>
public sealed class ApiReferenceTests
{
    [Fact]
    public async Task The_reference_page_is_answered_by_the_gateway_and_never_forwarded()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/docs/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        factory.Downstream.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_reference_page_needs_no_token()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/docs/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_reference_page_is_exempt_from_the_rate_limiter()
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
            using var response = await client.GetAsync("/docs/", TestContext.Current.CancellationToken);
            statuses.Add(response.StatusCode);
        }

        // The page and its script are several requests, and a reviewer reloading it must not use up the
        // budget that protects the API.
        statuses.ShouldAllBe(status => status == HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(CinemaApiDocuments.Catalog, "catalog")]
    [InlineData(CinemaApiDocuments.Bookings, "booking")]
    [InlineData(CinemaApiDocuments.Identity, "identity")]
    public async Task Every_document_the_page_lists_has_a_route_to_the_service_that_publishes_it(
        string document,
        string expectedCluster)
    {
        await using var factory = new GatewayFactory();

        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var route = configuration.GetSection($"ReverseProxy:Routes:openapi-{document}");

        // The names live in three places that cannot see each other: the service that publishes the
        // document, this routing table, and the page. A mismatch is a dropdown entry that loads nothing.
        route["Match:Path"].ShouldBe($"/openapi/{document}.json");
        route["ClusterId"].ShouldBe(expectedCluster);
        route["AuthorizationPolicy"].ShouldBe("Anonymous");
    }
}
