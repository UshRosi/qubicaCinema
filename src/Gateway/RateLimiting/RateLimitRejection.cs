using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace QubicaCinema.Gateway.RateLimiting;

/// <summary>What the gateway does to a request the limiter has just refused.</summary>
internal static class RateLimitRejection
{
    internal static ValueTask WriteRetryAfterAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            // Whole seconds, rounded up and never zero: "Retry-After: 0" sends the client straight back into
            // the wall it just hit.
            int seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }

        // No body on purpose. UseStatusCodePages turns the bare 429 into ProblemDetails on the way out, so
        // there is one writer for every error the gateway produces instead of two that can disagree.
        return ValueTask.CompletedTask;
    }
}
