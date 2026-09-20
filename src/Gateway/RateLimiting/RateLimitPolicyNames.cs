namespace QubicaCinema.Gateway.RateLimiting;

/// <summary>The names by which routes in <c>appsettings.json</c> refer to a rate-limiting policy.</summary>
internal static class RateLimitPolicyNames
{
    /// <summary>Must equal the <c>RateLimiterPolicy</c> of the <c>booking-create</c> route.</summary>
    internal const string BookingCreation = "booking-creation";
}
