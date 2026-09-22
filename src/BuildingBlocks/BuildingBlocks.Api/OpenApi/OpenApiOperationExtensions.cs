using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace QubicaCinema.BuildingBlocks.Api.OpenApi;

/// <summary>
/// Describes what a Minimal API endpoint does that its signature cannot say: the headers an endpoint filter
/// reads, the headers it writes, and worked examples of its bodies.
/// </summary>
/// <remarks>
/// Each method is one call to the framework's own <c>AddOpenApiOperationTransformer</c>; there is no
/// metadata of ours for a second transformer to read back. The names follow the rule the rest of the API
/// surface keeps: a gerund (<c>RequiringIfMatch</c>) installs behaviour, and <c>With…</c> only describes.
/// <para>
/// Examples are real instances, not JSON text. They are written out with the service's own serializer
/// options, so they use the camel-case names and string enums clients actually see, and renaming a property
/// breaks the build instead of leaving a stale example. Pass the whole request record: a polymorphic value
/// only carries its discriminator when it is written as its base type.
/// </para>
/// </remarks>
public static class OpenApiOperationExtensions
{
    /// <summary>Documents a request header as required, adding it when the endpoint does not bind it.</summary>
    public static RouteHandlerBuilder WithRequiredHeader(
        this RouteHandlerBuilder builder,
        string name,
        string description) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            OpenApiOperationEdits.RequireHeader(operation, name, description);

            return Task.CompletedTask;
        });

    /// <summary>Documents a header the endpoint writes on the response with the given status.</summary>
    public static RouteHandlerBuilder WithResponseHeader(
        this RouteHandlerBuilder builder,
        int statusCode,
        string name,
        string description) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            OpenApiOperationEdits.AddResponseHeader(operation, statusCode, name, description);

            return Task.CompletedTask;
        });

    /// <summary>
    /// Documents the <c>ETag</c> a response carries: the version a later update sends back as <c>If-Match</c>.
    /// </summary>
    public static RouteHandlerBuilder WithETagHeader(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status200OK) =>
        builder.WithResponseHeader(
            statusCode,
            HeaderNames.ETag,
            "The version of the resource. Send it back as If-Match to change it.");

    /// <summary>Documents the <c>Location</c> of a resource a <c>201 Created</c> has just made.</summary>
    public static RouteHandlerBuilder WithLocationHeader(this RouteHandlerBuilder builder) =>
        builder.WithResponseHeader(
            StatusCodes.Status201Created,
            HeaderNames.Location,
            "The address of the resource that was created.");

    /// <summary>Adds a named example of the request body.</summary>
    public static RouteHandlerBuilder WithRequestExample(
        this RouteHandlerBuilder builder,
        string name,
        string summary,
        object value) =>
        builder.AddOpenApiOperationTransformer((operation, context, _) =>
        {
            OpenApiOperationEdits.AddRequestExample(
                operation, name, summary, ToJson(value, context.ApplicationServices));

            return Task.CompletedTask;
        });

    /// <summary>Adds a named example of a problem-details response body.</summary>
    public static RouteHandlerBuilder WithResponseExample(
        this RouteHandlerBuilder builder,
        int statusCode,
        string name,
        string summary,
        object value) =>
        builder.AddOpenApiOperationTransformer((operation, context, _) =>
        {
            OpenApiOperationEdits.AddResponseExample(
                operation, statusCode, name, summary, ToJson(value, context.ApplicationServices));

            return Task.CompletedTask;
        });

    private static JsonNode ToJson(object value, IServiceProvider services)
    {
        JsonSerializerOptions options = services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;

        return JsonSerializer.SerializeToNode(value, value.GetType(), options)
            ?? throw new InvalidOperationException($"An example of type {value.GetType().Name} serialised to null.");
    }
}
