using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using QubicaCinema.BuildingBlocks.Api.Errors;

namespace QubicaCinema.BuildingBlocks.Api.Concurrency;

/// <summary>
/// Refuses an update that does not say which version it is replacing.
/// </summary>
/// <remarks>
/// Two different failures, two different codes, and the distinction is the point:
/// <list type="bullet">
/// <item><description>
/// no <c>If-Match</c> at all is <b>428 Precondition Required</b> (RFC 6585) — the request is not wrong, it
/// is incomplete, and answering 400 would tell a client to fix the body it got right;
/// </description></item>
/// <item><description>
/// an <c>If-Match</c> that is not a tag this service could have issued is <b>400</b>;
/// </description></item>
/// <item><description>
/// a well-formed but stale tag is <b>412</b>, and only the store can decide that — it surfaces later as a
/// <c>ConcurrentModificationException</c>.
/// </description></item>
/// </list>
/// Requiring the header rather than treating its absence as "overwrite anything" is deliberate: a lost
/// update is silent, and the only client that never sends the header is one that never read the resource.
/// </remarks>
public sealed class IfMatchFilter : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        string? ifMatch = context.HttpContext.Request.Headers[HeaderNames.IfMatch].ToString();

        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return TypedResults.Problem(
                detail: "This request must carry an If-Match header holding the ETag of the version being replaced.",
                statusCode: StatusCodes.Status428PreconditionRequired,
                title: "Precondition required",
                type: ProblemTypes.PreconditionRequired);
        }

        if (!ETag.TryParse(ifMatch, out _))
        {
            return TypedResults.Problem(
                detail: "The If-Match header is not an entity tag issued by this service.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Malformed If-Match",
                type: ProblemTypes.Invalid);
        }

        return await next(context);
    }
}
