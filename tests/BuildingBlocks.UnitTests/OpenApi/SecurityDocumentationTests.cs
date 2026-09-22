using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using QubicaCinema.BuildingBlocks.UnitTests.OpenApi.Fixtures;

namespace QubicaCinema.BuildingBlocks.UnitTests.OpenApi;

/// <summary>
/// The document says which operations need a token, from the same authorization metadata the service
/// enforces, so the two cannot disagree.
/// </summary>
public sealed class SecurityDocumentationTests
{
    [Fact]
    public async Task Should_declare_the_bearer_scheme_as_a_jwt_over_http()
    {
        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapGet("/orders", () => TypedResults.Ok(new SampleOrder(Guid.Empty, SampleStatus.Pending, []))));

        JsonNode document = await host.DocumentAsync();

        JsonNode scheme = document["components"]!["securitySchemes"]!["Bearer"]!;
        scheme["type"]!.GetValue<string>().ShouldBe("http");
        scheme["scheme"]!.GetValue<string>().ShouldBe("bearer");
        scheme["bearerFormat"]!.GetValue<string>().ShouldBe("JWT");
    }

    [Fact]
    public async Task Should_require_the_bearer_token_on_an_endpoint_that_requires_authorization()
    {
        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapGet("/secret", () => TypedResults.Ok()).RequireAuthorization());

        JsonNode operation = await host.OperationAsync("get", "/secret");

        // Asserted on the serialised JSON. A requirement whose scheme reference has lost its document is
        // written as [{}], which OpenAPI defines as "anonymous access is allowed": the opposite.
        JsonNode requirement = operation["security"]!.AsArray().ShouldHaveSingleItem()!;
        requirement["Bearer"].ShouldNotBeNull();
        requirement["Bearer"]!.AsArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_not_ask_for_a_token_on_an_endpoint_nobody_protected()
    {
        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapGet("/open", () => TypedResults.Ok()));

        (await host.OperationAsync("get", "/open"))["security"].ShouldBeNull();
    }

    [Fact]
    public async Task Should_not_ask_for_a_token_on_an_endpoint_that_says_it_is_anonymous()
    {
        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapGet("/login", () => TypedResults.Ok()).RequireAuthorization().AllowAnonymous());

        (await host.OperationAsync("get", "/login"))["security"].ShouldBeNull();
    }
}
