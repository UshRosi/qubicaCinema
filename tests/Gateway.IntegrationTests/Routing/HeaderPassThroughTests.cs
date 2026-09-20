using System.Net.Http.Headers;
using System.Text;
using QubicaCinema.Gateway.IntegrationTests.Fixtures;

namespace QubicaCinema.Gateway.IntegrationTests.Routing;

/// <summary>
/// The headers the services' contracts depend on. There are no transforms in the routing table, so this
/// pins that absence: adding one that drops or renames a header fails here.
/// </summary>
public sealed class HeaderPassThroughTests
{
    [Fact]
    public async Task The_request_headers_the_services_read_reach_them_untouched()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/bookings")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Idempotency-Key", "7d1c4e0a-6b0e-4f0e-9d6b-2f7f5f7f0a11");
        request.Headers.Add("X-User-Id", "0199a0c0-0000-7000-8000-00000000c0de");
        request.Headers.Add("X-User-Role", "Admin");
        request.Headers.TryAddWithoutValidation("If-Match", "\"abc\"");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        var forwarded = factory.Downstream.Requests.ShouldHaveSingleItem();
        forwarded.Header("Idempotency-Key").ShouldBe("7d1c4e0a-6b0e-4f0e-9d6b-2f7f5f7f0a11");
        forwarded.Header("X-User-Id").ShouldBe("0199a0c0-0000-7000-8000-00000000c0de");
        forwarded.Header("X-User-Role").ShouldBe("Admin");
        forwarded.Header("If-Match").ShouldBe("\"abc\"");
        forwarded.Header("Content-Type").ShouldNotBeNull().ShouldStartWith("application/json");
    }

    [Fact]
    public async Task The_response_headers_the_services_set_reach_the_client_untouched()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/screenings/{TestIds.Screening}/seats", TestContext.Current.CancellationToken);

        response.Headers.ETag.ShouldBe(new EntityTagHeaderValue("\"v1\""));
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        response.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe("/api/v1/bookings/abc");
    }

    [Fact]
    public async Task A_forwarded_header_a_client_forges_is_replaced_not_trusted()
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/movies");
        request.Headers.Add("X-Forwarded-Host", "attacker.example");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        // YARP sets the X-Forwarded-* family itself and drops what arrived; the value the service sees is
        // the gateway's own host, never the one the client claimed.
        factory.Downstream.Requests.ShouldHaveSingleItem()
            .Header("X-Forwarded-Host").ShouldNotBe("attacker.example");
    }
}
