using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace QubicaCinema.Gateway.RateLimiting;

/// <summary>The budget for <c>POST /api/v1/bookings</c>, named by that route's <c>RateLimiterPolicy</c>.</summary>
/// <remarks>
/// Built by the rate limiter through <c>ActivatorUtilities</c>, so its options come from the container rather
/// than from a closure. <see cref="IOptions{TOptions}"/> and not <see cref="IOptionsMonitor{TOptions}"/> on
/// purpose: a partition's limiter is created once and cached for the life of the process, so a reloaded window
/// would never reach the buckets already in flight and the monitor would promise something that does not happen.
/// </remarks>
internal sealed class BookingCreationRateLimitPolicy(IOptions<RateLimitOptions> options) : IRateLimiterPolicy<string>
{
    private readonly RateLimitOptions _limits = options.Value;

    /// <summary>Null, so the single handler on the limiter's own options answers for this policy too.</summary>
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientPartition.KeyFor(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = _limits.BookingCreationPermitLimit,
                Window = _limits.BookingCreationWindow,
                QueueLimit = 0,
            });
}
