using QubicaCinema.BuildingBlocks.Api.OpenApi;
using System.Net;
using System.Net.Http.Headers;
using QubicaCinema.BuildingBlocks.Api.Errors;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Gateway.IntegrationTests.Errors;
using QubicaCinema.Gateway.IntegrationTests.Fixtures;

namespace QubicaCinema.Gateway.IntegrationTests.Authorization;

/// <summary>
/// Which token a route asks for, and that a request without it goes no further than the gateway. This is early
/// rejection: the services check the token again, so what is pinned here is only that the first line holds.
/// </summary>
public sealed class AuthorizationTests
{
    [Theory]
    [InlineData("GET", "/api/v1/movies")]
    [InlineData("GET", $"/api/v1/movies/{TestIds.Movie}")]
    [InlineData("GET", "/api/v1/auditoriums")]
    [InlineData("GET", "/api/v1/screenings")]
    [InlineData("GET", $"/api/v1/screenings/{TestIds.Screening}")]
    [InlineData("GET", $"/api/v1/screenings/{TestIds.Screening}/seats")]
    [InlineData("POST", "/api/v1/auth/login")]
    [InlineData("POST", "/api/v1/auth/register")]
    [InlineData("GET", $"/openapi/{CinemaApiDocuments.Catalog}.json")]
    [InlineData("GET", $"/openapi/{CinemaApiDocuments.Bookings}.json")]
    [InlineData("GET", $"/openapi/{CinemaApiDocuments.Identity}.json")]
    public async Task Reading_the_programme_the_documentation_and_signing_in_need_no_token(string method, string path)
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Downstream.Requests.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("POST", "/api/v1/movies")]
    [InlineData("PUT", $"/api/v1/movies/{TestIds.Movie}")]
    [InlineData("POST", "/api/v1/auditoriums")]
    [InlineData("POST", "/api/v1/screenings")]
    [InlineData("PUT", $"/api/v1/screenings/{TestIds.Screening}")]
    [InlineData("POST", $"/api/v1/screenings/{TestIds.Screening}/cancellation")]
    [InlineData("POST", "/api/v1/bookings")]
    [InlineData("GET", "/api/v1/bookings")]
    [InlineData("GET", $"/api/v1/bookings/{TestIds.Booking}")]
    [InlineData("POST", $"/api/v1/bookings/{TestIds.Booking}/cancellation")]
    public async Task A_request_with_no_token_is_a_401_that_never_reaches_a_service(string method, string path)
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await GatewayErrorTests.ReadType(response)).ShouldBe(ProblemTypes.Unauthenticated);
        factory.Downstream.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("POST", "/api/v1/movies")]
    [InlineData("PUT", $"/api/v1/screenings/{TestIds.Screening}")]
    [InlineData("POST", $"/api/v1/screenings/{TestIds.Screening}/cancellation")]
    public async Task A_customer_is_refused_the_administrators_routes_with_a_403(string method, string path)
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClientFor(TestIds.Customer, CinemaRoles.Customer);

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await GatewayErrorTests.ReadType(response)).ShouldBe(ProblemTypes.Forbidden);
        factory.Downstream.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_administrator_may_write_to_the_catalogue()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClientFor(TestIds.Administrator, CinemaRoles.Admin);

        using var response = await client.PostAsync("/api/v1/movies", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Downstream.Requests.ShouldHaveSingleItem().Uri.Host.ShouldBe(GatewayFactory.CatalogHost);
    }

    [Fact]
    public async Task An_administrator_cannot_book_but_may_read_and_cancel_bookings()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClientFor(TestIds.Administrator, CinemaRoles.Admin);

        using var create = await client.PostAsync("/api/v1/bookings", content: null, TestContext.Current.CancellationToken);
        using var read = await client.GetAsync($"/api/v1/bookings/{TestIds.Booking}", TestContext.Current.CancellationToken);
        using var cancel = await client.PostAsync($"/api/v1/bookings/{TestIds.Booking}/cancellation", content: null, TestContext.Current.CancellationToken);

        // Creating needs the Customer role. The rest asks only for a token, so an administrator can act for the cinema.
        create.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        read.StatusCode.ShouldBe(HttpStatusCode.OK);
        cancel.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_customer_may_book()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClientFor(TestIds.Customer, CinemaRoles.Customer);

        using var response = await client.PostAsync("/api/v1/bookings", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_a_401()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();
        string forged = TestTokens.For(
            TestIds.Administrator, [CinemaRoles.Admin], signingKey: "some-other-key-that-is-also-long-enough-for-hmac");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        using var response = await client.PostAsync("/api/v1/movies", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Downstream.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_expired_token_is_a_401()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();
        // Expired an hour ago: well outside the thirty seconds of clock skew.
        string expired = TestTokens.For(TestIds.Administrator, [CinemaRoles.Admin], lifetime: TimeSpan.FromHours(-1));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);

        using var response = await client.PostAsync("/api/v1/movies", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Downstream.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_health_endpoints_stay_anonymous()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();

        using var health = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        using var alive = await client.GetAsync("/alive", TestContext.Current.CancellationToken);

        health.StatusCode.ShouldBe(HttpStatusCode.OK);
        alive.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
