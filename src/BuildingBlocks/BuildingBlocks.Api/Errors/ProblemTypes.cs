namespace QubicaCinema.BuildingBlocks.Api.Errors;

/// <summary>
/// The stable <c>type</c> URIs used in every ProblemDetails this solution returns.
/// </summary>
/// <remarks>
/// RFC 9457 makes the type URI the one part a client is allowed to branch on — the status code is coarse
/// and the title is prose that may be reworded or translated. So these strings are a published contract:
/// they are declared once, here, and never edited afterwards. They are not expected to resolve; the
/// authority is a placeholder for wherever the API documentation ends up being hosted.
/// </remarks>
public static class ProblemTypes
{
    private const string Base = "https://qubicacinema.dev/problems/";

    /// <summary>The thing being acted on does not exist, or the caller may not know that it does.</summary>
    public const string NotFound = $"{Base}not-found";

    /// <summary>The current state refuses a request that is otherwise well formed.</summary>
    public const string Conflict = $"{Base}conflict";

    /// <summary>The request breaks a rule decidable from its own content.</summary>
    public const string Invalid = $"{Base}invalid-request";

    /// <summary>The request could not even be read: malformed JSON, or a shape the service does not know.</summary>
    public const string MalformedRequest = $"{Base}malformed-request";

    /// <summary>The request did not pass validation.</summary>
    public const string ValidationFailed = $"{Base}validation-failed";

    /// <summary>The caller is not allowed to do this.</summary>
    public const string Forbidden = $"{Base}forbidden";

    /// <summary>The caller's <c>If-Match</c> was stale.</summary>
    public const string PreconditionFailed = $"{Base}precondition-failed";

    /// <summary>The request needs an <c>If-Match</c> and did not carry one.</summary>
    public const string PreconditionRequired = $"{Base}precondition-required";

    /// <summary>The request must carry a well-formed <c>Idempotency-Key</c> header and did not.</summary>
    public const string IdempotencyKeyRequired = $"{Base}idempotency-key-required";

    /// <summary>The <c>Idempotency-Key</c> was already used for a different request.</summary>
    public const string IdempotencyKeyReuse = $"{Base}idempotency-key-reuse";

    /// <summary>A request with the same <c>Idempotency-Key</c> is still being processed.</summary>
    public const string RequestInFlight = $"{Base}request-in-flight";

    /// <summary>Something went wrong that the caller cannot act on.</summary>
    public const string Unexpected = $"{Base}unexpected-error";
}
