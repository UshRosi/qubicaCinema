using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi;

namespace QubicaCinema.BuildingBlocks.Api.OpenApi;

/// <summary>
/// The edits <see cref="OpenApiOperationExtensions"/> make to an operation, written against the OpenAPI.NET
/// v2 model.
/// </summary>
/// <remarks>
/// Kept apart from the extensions because the v2 model is read-only through its interfaces and nullable
/// almost everywhere: a header parameter or a response header can only be written through the concrete
/// types, and an operation with nothing but a body has no parameter list at all. That handling is the same
/// for every edit, so it lives once here and the extensions stay one line each.
/// </remarks>
internal static class OpenApiOperationEdits
{
    internal const string JsonContentType = "application/json";
    internal const string ProblemContentType = "application/problem+json";

    /// <summary>Declares a header the endpoint cannot bind, or upgrades one it binds as optional.</summary>
    internal static void RequireHeader(OpenApiOperation operation, string name, string description)
    {
        IList<IOpenApiParameter> parameters = operation.Parameters ??= [];

        var header = new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Header,
            Required = true,
            Description = description,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        };

        int existing = IndexOfHeader(parameters, name);
        if (existing >= 0)
        {
            parameters[existing] = header;
        }
        else
        {
            parameters.Add(header);
        }
    }

    /// <summary>Declares a header the endpoint writes on one of its responses.</summary>
    internal static void AddResponseHeader(OpenApiOperation operation, int statusCode, string name, string description)
    {
        OpenApiResponse response = ResponseFor(operation, statusCode);

        response.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.Ordinal);
        response.Headers[name] = new OpenApiHeader
        {
            Description = description,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        };
    }

    /// <summary>Adds a named example to the request body.</summary>
    internal static void AddRequestExample(OpenApiOperation operation, string name, string summary, JsonNode value)
    {
        if (operation.RequestBody is not OpenApiRequestBody body)
        {
            throw new InvalidOperationException(
                $"'{operation.OperationId}' has no request body to attach the '{name}' example to.");
        }

        body.Content ??= new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal);
        AddExample(body.Content, JsonContentType, name, summary, value);
    }

    /// <summary>Adds a named example to a response body.</summary>
    internal static void AddResponseExample(
        OpenApiOperation operation,
        int statusCode,
        string name,
        string summary,
        JsonNode value)
    {
        OpenApiResponse response = ResponseFor(operation, statusCode);

        response.Content ??= new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal);
        AddExample(response.Content, ProblemContentType, name, summary, value);
    }

    private static void AddExample(
        IDictionary<string, OpenApiMediaType> content,
        string contentType,
        string name,
        string summary,
        JsonNode value)
    {
        if (!content.TryGetValue(contentType, out OpenApiMediaType? mediaType))
        {
            mediaType = new OpenApiMediaType();
            content[contentType] = mediaType;
        }

        mediaType.Examples ??= new Dictionary<string, IOpenApiExample>(StringComparer.Ordinal);
        mediaType.Examples[name] = new OpenApiExample { Summary = summary, Value = value };
    }

    private static OpenApiResponse ResponseFor(OpenApiOperation operation, int statusCode)
    {
        operation.Responses ??= new OpenApiResponses();

        string key = statusCode.ToString(CultureInfo.InvariantCulture);
        if (operation.Responses.TryGetValue(key, out IOpenApiResponse? existing))
        {
            return existing as OpenApiResponse
                ?? throw new InvalidOperationException(
                    $"The {key} response of '{operation.OperationId}' is a reference and cannot be edited in place.");
        }

        var created = new OpenApiResponse { Description = ReasonPhrases.GetReasonPhrase(statusCode) };
        operation.Responses[key] = created;

        return created;
    }

    private static int IndexOfHeader(IList<IOpenApiParameter> parameters, string name)
    {
        for (int index = 0; index < parameters.Count; index++)
        {
            IOpenApiParameter parameter = parameters[index];
            if (parameter.In == ParameterLocation.Header
                && string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
}
