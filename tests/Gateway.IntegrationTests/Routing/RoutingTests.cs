using System.Net;
using QubicaCinema.Gateway.IntegrationTests.Fixtures;

namespace QubicaCinema.Gateway.IntegrationTests.Routing;

/// <summary>
/// Which service each public path reaches. The interesting rows are the two under /screenings, which the
/// routing table splits between services without ordering either route.
/// </summary>
public sealed class RoutingTests
{
    [Theory]
    [InlineData("GET", "/api/v1/movies", GatewayFactory.CatalogHost)]
    [InlineData("GET", "/api/v1/movies?page=2&pageSize=10", GatewayFactory.CatalogHost)]
    [InlineData("GET", $"/api/v1/movies/{TestIds.Movie}", GatewayFactory.CatalogHost)]
    [InlineData("PUT", $"/api/v1/movies/{TestIds.Movie}", GatewayFactory.CatalogHost)]
    [InlineData("GET", "/api/v1/auditoriums", GatewayFactory.CatalogHost)]
    [InlineData("GET", "/api/v1/screenings", GatewayFactory.CatalogHost)]
    [InlineData("GET", $"/api/v1/screenings/{TestIds.Screening}", GatewayFactory.CatalogHost)]
    [InlineData("POST", $"/api/v1/screenings/{TestIds.Screening}/cancellation", GatewayFactory.CatalogHost)]
    [InlineData("GET", "/api/v1/bookings", GatewayFactory.BookingHost)]
    [InlineData("POST", "/api/v1/bookings", GatewayFactory.BookingHost)]
    [InlineData("GET", $"/api/v1/bookings/{TestIds.Booking}", GatewayFactory.BookingHost)]
    [InlineData("POST", $"/api/v1/bookings/{TestIds.Booking}/cancellation", GatewayFactory.BookingHost)]
    [InlineData("POST", $"/api/v1/bookings/{TestIds.Booking}/items/{TestIds.BookingItem}/cancellation", GatewayFactory.BookingHost)]
    public async Task A_path_reaches_the_service_that_owns_it_and_arrives_unchanged(
        string method,
        string path,
        string expectedHost)
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var forwarded = factory.Downstream.Requests.ShouldHaveSingleItem();
        forwarded.Uri.Host.ShouldBe(expectedHost);
        // The whole path and the query string, byte for byte: the services emit absolute-path Location
        // headers, which are only correct if the gateway never rewrites the prefix.
        forwarded.Uri.PathAndQuery.ShouldBe(path);
    }

    [Fact]
    public async Task The_seat_map_goes_to_booking_although_the_rest_of_screenings_goes_to_catalog()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();

        await client.GetAsync($"/api/v1/screenings/{TestIds.Screening}", TestContext.Current.CancellationToken);
        await client.GetAsync($"/api/v1/screenings/{TestIds.Screening}/seats", TestContext.Current.CancellationToken);

        // The test that fails the moment somebody puts an "Order" on a route: Order is compared before
        // route-template precedence, so it would let a catch-all swallow the more specific path.
        factory.Downstream.Requests.Select(request => request.Uri.Host)
            .ShouldBe([GatewayFactory.CatalogHost, GatewayFactory.BookingHost]);
    }
}
