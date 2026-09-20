using System.Net;
using System.Text;
using System.Text.Json;
using QubicaCinema.BuildingBlocks.Api.Errors;
using QubicaCinema.Gateway.IntegrationTests.Fixtures;

namespace QubicaCinema.Gateway.IntegrationTests.Errors;

/// <summary>
/// One error shape whether the answer came from the gateway or from a service: RFC 9457 with the stable
/// type URIs the services already use.
/// </summary>
public sealed class GatewayErrorTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/nope")]
    [InlineData("/api/v2/movies")]
    public async Task A_path_no_route_matches_is_a_problem_details_404(string path)
    {
        await using var factory = new GatewayFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await ReadType(response)).ShouldBe(ProblemTypes.NotFound);
        factory.Downstream.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_service_that_cannot_be_reached_is_a_problem_details_502()
    {
        await using var factory = new GatewayFactory();
        factory.Downstream.Respond = _ => throw new HttpRequestException("connection refused");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/movies", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await ReadType(response)).ShouldBe(ProblemTypes.UpstreamFailed);
    }

    [Fact]
    public async Task An_error_a_service_produced_is_passed_through_not_rewritten()
    {
        await using var factory = new GatewayFactory();
        factory.Downstream.Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"type":"https://qubicacinema.dev/problems/not-found","title":"Screening not found","status":404}""",
                Encoding.UTF8,
                "application/problem+json"),
        };
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/screenings/{TestIds.Screening}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        // The gateway's own 404 would say "Not found". This one says what Catalog said.
        body.GetProperty("title").GetString().ShouldBe("Screening not found");
    }

    internal static async Task<string?> ReadType(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return body.GetProperty("type").GetString();
    }
}
