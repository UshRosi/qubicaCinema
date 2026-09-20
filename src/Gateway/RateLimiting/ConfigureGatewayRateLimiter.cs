using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace QubicaCinema.Gateway.RateLimiting;

/// <summary>Configures the rate-limiting middleware from <see cref="RateLimitOptions"/>.</summary>
/// <remarks>
/// An <see cref="IConfigureOptions{TOptions}"/> rather than the <c>AddRateLimiter(Action)</c> overload: that
/// overload has no service provider, so the numbers would have to be read out of configuration by hand and
/// would escape <c>ValidateOnStart</c>.
/// </remarks>
internal sealed class ConfigureGatewayRateLimiter(IOptions<RateLimitOptions> options)
    : IConfigureOptions<RateLimiterOptions>
{
    public void Configure(RateLimiterOptions limiter)
    {
        RateLimitOptions limits = options.Value;

        // 503 is the framework default and it is the wrong answer: it tells a client the server is broken when
        // the truth is that the client asked too often, which is something it can act on.
        limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        limiter.OnRejected = RateLimitRejection.WriteRetryAfterAsync;

        // The floor under every route, including ones added after this file was last read. Endpoints marked
        // DisableRateLimiting (/health and /alive) skip it entirely: the middleware checks for that attribute
        // before it ever consults the global limiter.
        limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                ClientPartition.KeyFor(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.PermitLimit,
                    Window = limits.Window,

                    // Queueing turns a fast 429 into a slow one and holds a connection while it waits.
                    QueueLimit = 0,
                }));

        limiter.AddPolicy<string, BookingCreationRateLimitPolicy>(RateLimitPolicyNames.BookingCreation);
    }
}
