namespace QubicaCinema.Gateway.RateLimiting;

/// <summary>How hard a single client may hit the gateway. Checked by <see cref="RateLimitOptionsValidator"/>.</summary>
internal sealed class RateLimitOptions
{
    internal const string SectionName = "RateLimiting";

    /// <summary>Requests one client may make inside <see cref="Window"/>, across every route.</summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>The window <see cref="PermitLimit"/> is counted in.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Bookings one client may create inside <see cref="BookingCreationWindow"/>.</summary>
    public int BookingCreationPermitLimit { get; set; } = 10;

    /// <summary>The window <see cref="BookingCreationPermitLimit"/> is counted in.</summary>
    public TimeSpan BookingCreationWindow { get; set; } = TimeSpan.FromSeconds(10);
}
