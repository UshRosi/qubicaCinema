using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QubicaCinema.BuildingBlocks.Api.Errors;
using QubicaCinema.BuildingBlocks.Api.OpenApi;

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
    /// <para>
    /// The filter's own answers are documented as examples and not as a schema. Its 422 (a key reused for a
    /// different body) shares a status code with validation's 422, and the framework keeps one schema per
    /// status: declaring a plain <c>ProblemDetails</c> here would replace validation's <c>errors</c> map in
    /// the document. Examples let both shapes appear under the one status.
    /// </para>
    /// </remarks>
    public static RouteHandlerBuilder RequiringIdempotencyKey<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : notnull =>
        builder
            .AddEndpointFilter<IdempotencyFilter<TRequest>>()
            .WithRequiredHeader(
                IdempotencyFilter<TRequest>.HeaderName,
                "A value generated once per logical request, such as a UUID, and reused on every retry. Repeating "
                + "a request with the same key and body returns the original response instead of running it again.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithResponseExample(
                StatusCodes.Status400BadRequest,
                "idempotency-key-required",
                "The Idempotency-Key header is missing or malformed",
                new ProblemDetails
                {
                    Type = ProblemTypes.IdempotencyKeyRequired,
                    Title = "Idempotency key required",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"This request must carry an {IdempotencyFilter<TRequest>.HeaderName} header of 1 to "
                             + $"{IdempotencyFilter<TRequest>.MaxKeyLength} visible ASCII characters, for example a UUID.",
                })
            .WithResponseExample(
                StatusCodes.Status409Conflict,
                "request-in-flight",
                "The same key is still being processed",
                new ProblemDetails
                {
                    Type = ProblemTypes.RequestInFlight,
                    Title = "Request in flight",
                    Status = StatusCodes.Status409Conflict,
                    Detail = $"A request with {IdempotencyFilter<TRequest>.HeaderName} '…' is still being processed. "
                             + "Retry shortly to receive its result.",
                })
            .WithResponseExample(
                StatusCodes.Status422UnprocessableEntity,
                "idempotency-key-reuse",
                "The key was already used for a different request",
                new ProblemDetails
                {
                    Type = ProblemTypes.IdempotencyKeyReuse,
                    Title = "Idempotency key reused",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = $"The {IdempotencyFilter<TRequest>.HeaderName} '…' was already used for a different request. "
                             + "Use a new key for a new request.",
                });
}
