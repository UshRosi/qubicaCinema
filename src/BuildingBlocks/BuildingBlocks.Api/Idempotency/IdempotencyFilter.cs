using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using QubicaCinema.BuildingBlocks.Api.Errors;
using QubicaCinema.BuildingBlocks.Application.Idempotency;
using QubicaCinema.BuildingBlocks.Application.Security;

namespace QubicaCinema.BuildingBlocks.Api.Idempotency;

/// <summary>
/// Makes a POST safe to retry: the same <c>Idempotency-Key</c> never runs the endpoint twice.
/// </summary>
/// <remarks>
/// An endpoint filter rather than code in the handler, because "at most once" is a property of the HTTP
/// exchange — a client whose connection dropped does not know whether its booking was made — and not of
/// the use case. The handler stays unaware of it and the route states it in one line.
/// <list type="bullet">
/// <item><description>an unseen key runs the endpoint and records its response in the same transaction;</description></item>
/// <item><description>the same key and the same body answer the recorded response with <b>200</b> and
/// <c>Idempotency-Replayed: true</c>, rather than making a second booking;</description></item>
/// <item><description>the same key with a different body is <b>422</b>: that is a client bug, and answering
/// with the first request's response would hide it;</description></item>
/// <item><description>the same key still being processed is <b>409</b> with <c>Retry-After: 1</c>;</description></item>
/// <item><description>no key, or a malformed one, is <b>400</b>, as the IETF idempotency-key draft says.</description></item>
/// </list>
/// The fingerprint is taken over the request as bound and re-serialised, not over the raw bytes: two
/// bodies that differ only in whitespace or property order are the same request, and the raw body has
/// already been consumed by the time a filter runs.
/// </remarks>
/// <typeparam name="TRequest">The request body the fingerprint is taken over.</typeparam>
public sealed class IdempotencyFilter<TRequest>(
    IIdempotencyStore store,
    ICurrentUser currentUser,
    IOptions<JsonOptions> jsonOptions) : IEndpointFilter
    where TRequest : notnull
{
    /// <summary>The request header that carries the key.</summary>
    public const string HeaderName = "Idempotency-Key";

    /// <summary>The response header that marks an answer as a replay.</summary>
    public const string ReplayedHeaderName = "Idempotency-Replayed";

    /// <summary>The longest key accepted. A UUID is 36 characters; this leaves room for a client's own scheme.</summary>
    public const int MaxKeyLength = 100;

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        HttpContext httpContext = context.HttpContext;
        string key = httpContext.Request.Headers[HeaderName].ToString();

        if (!IsWellFormed(key))
        {
            return TypedResults.Problem(
                detail: $"This request must carry an {HeaderName} header of 1 to {MaxKeyLength} visible ASCII characters, "
                        + "for example a UUID generated once per logical request and reused on every retry.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Idempotency key required",
                type: ProblemTypes.IdempotencyKeyRequired);
        }

        if (context.Arguments.OfType<TRequest>().FirstOrDefault() is not { } request)
        {
            throw new InvalidOperationException(
                $"No argument of type {typeof(TRequest).Name} was bound, so no fingerprint can be taken.");
        }

        // The endpoint's own result is kept for the first execution, so it answers exactly as it would
        // without this filter — Location header included. Only a replay is rebuilt from the record.
        object? executedResult = null;

        IdempotencyOutcome outcome = await store.ExecuteOnceAsync(
            new IdempotentRequest(currentUser.UserId, key, Fingerprint(request)),
            async _ =>
            {
                executedResult = await next(context);
                return Record(executedResult);
            },
            httpContext.RequestAborted);

        return outcome switch
        {
            IdempotencyOutcome.Executed => executedResult,
            IdempotencyOutcome.Replayed replayed => Replay(replayed.Response, httpContext),
            IdempotencyOutcome.KeyReused => TypedResults.Problem(
                detail: $"The {HeaderName} '{key}' was already used for a different request. Use a new key for a new request.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Idempotency key reused",
                type: ProblemTypes.IdempotencyKeyReuse),
            IdempotencyOutcome.InFlight => InFlight(key, httpContext),

            // The union is closed, so this arm is unreachable; the compiler cannot know that.
            _ => throw new InvalidOperationException($"Unknown idempotency outcome {outcome.GetType().Name}."),
        };
    }

    private static bool IsWellFormed(string key) =>
        key.Length is > 0 and <= MaxKeyLength && key.All(character => character is >= '!' and <= '~');

    private string Fingerprint(TRequest request)
    {
        byte[] canonical = JsonSerializer.SerializeToUtf8Bytes(request, jsonOptions.Value.SerializerOptions);

        return Convert.ToHexStringLower(SHA256.HashData(canonical));
    }

    /// <summary>Captures what the endpoint answered, so that it can be stored and replayed.</summary>
    private RecordedResponse Record(object? result)
    {
        int statusCode = result is IStatusCodeHttpResult { StatusCode: { } code } ? code : StatusCodes.Status200OK;
        object? value = result is IValueHttpResult valueResult ? valueResult.Value : null;

        string body = value is null
            ? string.Empty
            : JsonSerializer.Serialize(value, value.GetType(), jsonOptions.Value.SerializerOptions);

        return new RecordedResponse(statusCode, body);
    }

    /// <summary>
    /// Answers a retry with the recorded body. 200 rather than the original 201: nothing was created by
    /// this request, and the header tells the client why it got a body without having made anything.
    /// </summary>
    private static ContentHttpResult Replay(RecordedResponse response, HttpContext httpContext)
    {
        httpContext.Response.Headers[ReplayedHeaderName] = "true";

        return TypedResults.Text(response.Body, "application/json", Encoding.UTF8, StatusCodes.Status200OK);
    }

    private static ProblemHttpResult InFlight(string key, HttpContext httpContext)
    {
        httpContext.Response.Headers.RetryAfter = "1";

        return TypedResults.Problem(
            detail: $"A request with {HeaderName} '{key}' is still being processed. Retry shortly to receive its result.",
            statusCode: StatusCodes.Status409Conflict,
            title: "Request in flight",
            type: ProblemTypes.RequestInFlight);
    }
}
