using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QubicaCinema.BuildingBlocks.Api.OpenApi;

namespace QubicaCinema.BuildingBlocks.UnitTests.OpenApi;

/// <summary>
/// A few routes in memory, wired the way each service wires its OpenAPI document, and the document read back
/// as JSON.
/// </summary>
/// <remarks>
/// The tests read the <em>serialised</em> document on purpose. The object model can look right and still be
/// written wrongly: a security requirement whose scheme reference has lost its document, for one, is
/// serialised as an empty requirement, which OpenAPI reads as "no token needed".
/// </remarks>
internal sealed class OpenApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private OpenApiTestHost(WebApplication app) => _app = app;

    /// <summary>Starts a host, maps the given routes, and publishes the document as a service would.</summary>
    internal static async Task<OpenApiTestHost> StartAsync(Action<IEndpointRouteBuilder> mapRoutes)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["OpenApi:Expose"] = "true" });

        // The serializer options every service configures, because that is what examples must be written with.
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.AddCinemaOpenApi(CinemaApiDocuments.Catalog, "Test API", "A test document.");

        WebApplication app = builder.Build();
        app.MapCinemaOpenApi();
        mapRoutes(app);

        await app.StartAsync(TestContext.Current.CancellationToken);

        return new OpenApiTestHost(app);
    }

    /// <summary>The document, as a client would read it.</summary>
    internal async Task<JsonNode> DocumentAsync()
    {
        string json = await _app.GetTestClient()
            .GetStringAsync($"/openapi/{CinemaApiDocuments.Catalog}.json", TestContext.Current.CancellationToken);

        return JsonNode.Parse(json)!;
    }

    /// <summary>The operation for one route, read out of the document.</summary>
    internal async Task<JsonNode> OperationAsync(string method, string path)
    {
        JsonNode document = await DocumentAsync();

        return document["paths"]![path]![method]!;
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
