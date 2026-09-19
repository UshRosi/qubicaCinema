using QubicaCinema.BuildingBlocks.Application.Idempotency;

namespace QubicaCinema.BuildingBlocks.Persistence.Idempotency;

/// <summary>
/// One idempotency key a customer has used, and the response its request produced.
/// </summary>
/// <remarks>
/// A persistence record, not a domain entity: it says nothing about cinemas, only about which HTTP requests
/// have already been carried out — which is why it is a building block rather than part of a service's
/// model. It still guards its own state: claimed, then completed exactly once.
/// </remarks>
public sealed class IdempotencyRecord
{
    private IdempotencyRecord(Guid userId, string key, string requestHash, DateTimeOffset createdAt)
    {
        UserId = userId;
        Key = key;
        RequestHash = requestHash;
        CreatedAt = createdAt;
    }

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private IdempotencyRecord()
    {
        Key = string.Empty;
        RequestHash = string.Empty;
    }

    /// <summary>Whose key it is.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The key, as the client sent it.</summary>
    public string Key { get; private set; }

    /// <summary>The fingerprint of the request the key was first used for.</summary>
    public string RequestHash { get; private set; }

    /// <summary>The status code of the recorded response, once there is one.</summary>
    public int? StatusCode { get; private set; }

    /// <summary>The body of the recorded response, once there is one.</summary>
    public string? ResponseBody { get; private set; }

    /// <summary>When the key was first used.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Claims a key for a request about to run.</summary>
    public static IdempotencyRecord Claim(IdempotentRequest request, DateTimeOffset now) =>
        new(request.UserId, request.Key, request.RequestHash, now);

    /// <summary>Records what the request answered.</summary>
    /// <exception cref="InvalidOperationException">The record was already completed.</exception>
    public void Complete(RecordedResponse response)
    {
        if (StatusCode is not null)
        {
            throw new InvalidOperationException($"Idempotency key '{Key}' already holds a response.");
        }

        StatusCode = response.StatusCode;
        ResponseBody = response.Body;
    }

    /// <summary>What a second request with the same key should be told.</summary>
    public IdempotencyOutcome AnswerRepeat(string requestHash)
    {
        if (!string.Equals(RequestHash, requestHash, StringComparison.Ordinal))
        {
            return new IdempotencyOutcome.KeyReused();
        }

        // A committed record always has its response; an incomplete one is visible only to a reader that
        // does not honour transactions, and "still in flight" is the honest answer to give it.
        return StatusCode is { } statusCode && ResponseBody is { } body
            ? new IdempotencyOutcome.Replayed(new RecordedResponse(statusCode, body))
            : new IdempotencyOutcome.InFlight();
    }
}
