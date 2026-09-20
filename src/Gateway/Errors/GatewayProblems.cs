using QubicaCinema.BuildingBlocks.Api.Errors;

namespace QubicaCinema.Gateway.Errors;

/// <summary>
/// Gives every status the gateway produces itself the same RFC 9457 vocabulary the services use.
/// </summary>
/// <remarks>
/// Invoked by the framework's problem-details writer after it has applied its own defaults, which put an
/// RFC 9110 section link in <c>type</c> for the statuses it knows and nothing at all for 429, 502, 503 and
/// 504. Both cases are overwritten: a client should branch on one vocabulary, not two.
/// <para>
/// This only runs for a response this process wrote. A 404 forwarded from Catalog arrives with Catalog's own
/// ProblemDetails body and a content type, so the status-code middleware leaves it alone.
/// </para>
/// </remarks>
internal static class GatewayProblems
{
    internal static void Describe(ProblemDetailsContext context)
    {
        if (Describe(context.HttpContext.Response.StatusCode) is not var (type, title, detail))
        {
            return;
        }

        context.ProblemDetails.Type = type;
        context.ProblemDetails.Title = title;
        context.ProblemDetails.Detail = detail;
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
    }

    private static (string Type, string Title, string Detail)? Describe(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound => (
            ProblemTypes.NotFound,
            "Not found",
            "No route at this gateway matches this path."),

        StatusCodes.Status429TooManyRequests => (
            ProblemTypes.TooManyRequests,
            "Too many requests",
            "The rate limit for this client was exceeded. Retry-After says how long to wait."),

        // Three upstream failures, three URIs, because the advice to the client differs. 502: the service
        // answered badly, so do not retry blindly. 503: nothing healthy is listening, so retry later.
        // 504: it was too slow and may have committed anyway.
        StatusCodes.Status502BadGateway => (
            ProblemTypes.UpstreamFailed,
            "Bad gateway",
            "The service that owns this path could not be reached or answered unusably."),

        StatusCodes.Status503ServiceUnavailable => (
            ProblemTypes.UpstreamUnavailable,
            "Service unavailable",
            "No healthy instance of the service that owns this path. Try again shortly."),

        StatusCodes.Status504GatewayTimeout => (
            ProblemTypes.UpstreamTimeout,
            "Gateway timeout",
            "The service did not answer in time. A write may still have been applied: replay it with the " +
            "same Idempotency-Key rather than composing a new request."),

        StatusCodes.Status500InternalServerError => (
            ProblemTypes.Unexpected,
            "Unexpected error",
            "Something went wrong in the gateway."),

        // Every other status is either one the gateway never produces or a proxied response that already
        // carries the service's own body. Leaving it alone is the point.
        _ => null,
    };
}
