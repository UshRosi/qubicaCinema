using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace QubicaCinema.BuildingBlocks.Api.Concurrency;

/// <summary>Attaches optimistic-concurrency behaviour to a route.</summary>
public static class ConcurrencyEndpointExtensions
{
    /// <summary>
    /// Requires a well-formed <c>If-Match</c> header, so the endpoint itself can assume one is there.
    /// </summary>
    public static RouteHandlerBuilder RequiringIfMatch(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilter<IfMatchFilter>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired);

    /// <summary>Puts the current version of a resource in the <c>ETag</c> response header.</summary>
    public static void SetETag(this HttpResponse response, ReadOnlyMemory<byte> version) =>
        response.Headers[HeaderNames.ETag] = ETag.Format(version);
}
