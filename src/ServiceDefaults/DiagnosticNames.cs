namespace QubicaCinema.ServiceDefaults;

/// <summary>
/// Names of the <see cref="System.Diagnostics.ActivitySource"/> and
/// <see cref="System.Diagnostics.Metrics.Meter"/> instances the solution owns.
/// Declared once so that producers and the telemetry configuration cannot drift apart.
/// </summary>
/// <remarks>
/// The event bus has no entry here: its source, <c>QubicaCinema.EventBus</c>, is declared in the RabbitMQ
/// building block that owns it, which cannot reference this project. It is picked up by the wildcard.
/// </remarks>
public static class DiagnosticNames
{
    /// <summary>Root name; every other name in the solution is a suffix of it.</summary>
    public const string Root = "QubicaCinema";

    /// <summary>The Booking bounded context.</summary>
    public const string Booking = $"{Root}.Booking";

    /// <summary>The Catalog bounded context.</summary>
    public const string Catalog = $"{Root}.Catalog";

    /// <summary>Wildcard subscribing to every source and meter above at once.</summary>
    internal const string Wildcard = $"{Root}.*";
}
