using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace QubicaCinema.BuildingBlocks.Api.Idempotency;

/// <summary>Attaches idempotency to a route.</summary>
public static class IdempotencyEndpointExtensions
{
    /// <summary>
    /// Requires an <c>Idempotency-Key</c> header and runs the endpoint at most once per key.
    /// </summary>
    /// <remarks>
    /// Add it after <c>ValidatingBody</c>: filters run in the order they are added, and a request that fails
    /// validation should be rejected before it is allowed to claim a key.
    /// </remarks>
    public static RouteHandlerBuilder RequiringIdempotencyKey<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : notnull =>
        builder
            .AddEndpointFilter<IdempotencyFilter<TRequest>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
