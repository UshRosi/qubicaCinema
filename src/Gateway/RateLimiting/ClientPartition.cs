using QubicaCinema.BuildingBlocks.Authentication;

namespace QubicaCinema.Gateway.RateLimiting;

/// <summary>Decides which bucket a request is counted in: one per client.</summary>
internal static class ClientPartition
{
    private const string Unknown = "unknown";

    // A signed-in caller is counted by their user id, so one account cannot multiply its budget by changing
    // address, and two accounts behind one address do not share a budget. Anyone without a valid token, which
    // includes every anonymous read, is counted by address as before.
    //
    // The subject is read from the principal that bearer validation produced, never from a header: a header is
    // anything the caller writes, and a limit keyed on it would be opt-out. The two kinds of key carry a prefix
    // so a user id can never collide with an address.
    internal static string KeyFor(HttpContext context) =>
        context.User.FindFirst(CinemaClaimTypes.Subject)?.Value is { Length: > 0 } subject
            ? $"user:{subject}"
            : $"ip:{AddressOf(context)}";

    private static string AddressOf(HttpContext context) =>
        // MapToIPv6 so that 127.0.0.1 and ::ffff:127.0.0.1, the same client seen through a dual-stack
        // socket, share one bucket instead of two.
        context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? Unknown;
}
