namespace QubicaCinema.Gateway.RateLimiting;

/// <summary>Decides which bucket a request is counted in: one per client.</summary>
internal static class ClientPartition
{
    private const string Unknown = "unknown";

    // TODO(chapter 5): prefer the validated token's subject, so one account cannot multiply its budget by
    // changing address. The address stays as the fallback for anonymous reads, which remain anonymous.
    //
    // The X-User-Id header is not usable here: it is chapter 2's stand-in identity, any caller can set it,
    // and a limit keyed on it would be opt-out.
    internal static string KeyFor(HttpContext context) =>
        // MapToIPv6 so that 127.0.0.1 and ::ffff:127.0.0.1, the same client seen through a dual-stack
        // socket, share one bucket instead of two.
        context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? Unknown;
}
