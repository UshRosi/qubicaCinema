using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using QubicaCinema.BuildingBlocks.Api.OpenApi;

namespace QubicaCinema.BuildingBlocks.Api.Concurrency;

/// <summary>Attaches optimistic-concurrency behaviour to a route.</summary>
public static class ConcurrencyEndpointExtensions
{
    /// <summary>
    /// Requires a well-formed <c>If-Match</c> header, so the endpoint itself can assume one is there.
    /// </summary>
    /// <remarks>
    /// The handlers bind the header as a nullable string on purpose, so the framework would describe it as
    /// optional. It is documented as required here because the filter answers 428 without it.
    /// </remarks>
    public static RouteHandlerBuilder RequiringIfMatch(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilter<IfMatchFilter>()
            .WithRequiredHeader(
                HeaderNames.IfMatch,
                "The ETag of the version being replaced, exactly as the GET returned it, quotes included.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired);

    /// <summary>Puts the current version of a resource in the <c>ETag</c> response header.</summary>
    public static void SetETag(this HttpResponse response, ReadOnlyMemory<byte> version) =>
        response.Headers[HeaderNames.ETag] = ETag.Format(version);
}
