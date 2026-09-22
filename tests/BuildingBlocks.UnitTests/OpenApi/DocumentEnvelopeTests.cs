using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using QubicaCinema.BuildingBlocks.Api.OpenApi;
using QubicaCinema.BuildingBlocks.UnitTests.Authentication;

namespace QubicaCinema.BuildingBlocks.UnitTests.OpenApi;

/// <summary>What surrounds the operations: who the document is about, and where it says the API lives.</summary>
public sealed class DocumentEnvelopeTests
{
    [Fact]
    public async Task Should_name_the_document_after_the_service()
    {
        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapGet("/orders", () => TypedResults.Ok()));

        JsonNode info = (await host.DocumentAsync())["info"]!;

        info["title"]!.GetValue<string>().ShouldBe("Test API");
        info["description"]!.GetValue<string>().ShouldBe("A test document.");
        info["version"]!.GetValue<string>().ShouldBe("v1");
    }

    [Fact]
    public async Task Should_list_no_server_so_that_a_try_it_call_goes_back_through_the_gateway()
    {
        // Left to itself the generator writes the address the request arrived on, which behind the gateway
        // is the service's own, and a "Try it" call would skip the rate limit and the first check of the token.
        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapGet("/orders", () => TypedResults.Ok()));

        JsonNode document = await host.DocumentAsync();

        (document["servers"] is null || document["servers"]!.AsArray().Count == 0).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Development", null, true)]
    [InlineData("Production", null, false)]
    [InlineData("Production", "true", true)]
    [InlineData("Production", "false", false)]
    public void Should_publish_in_development_or_when_asked_to(string environment, string? expose, bool expected)
    {
        Dictionary<string, string?> settings = expose is null ? [] : new() { ["OpenApi:Expose"] = expose };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        OpenApiEndpointOptions.ShouldExpose(new StubHostEnvironment(environment), configuration).ShouldBe(expected);
    }

    [Fact]
    public async Task Should_not_map_the_document_in_production_unless_asked()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Services.AddCinemaOpenApi(CinemaApiDocuments.Catalog, "Test API", "A test document.");

        await using WebApplication app = builder.Build();
        app.MapCinemaOpenApi();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await app.GetTestClient()
            .GetAsync($"/openapi/{CinemaApiDocuments.Catalog}.json", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
