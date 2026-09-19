namespace QubicaCinema.Catalog.Domain.Screenings;

/// <summary>
/// Whether a screening is still going to happen.
/// </summary>
/// <remarks>
/// There is no <c>Deleted</c>. Bookings in another service point at screening ids, so a screening is
/// cancelled and kept: a row that disappears would leave those bookings referring to nothing.
/// </remarks>
public enum ScreeningStatus
{
    /// <summary>It is going to happen.</summary>
    Scheduled = 0,

    /// <summary>It was called off. The screening stays readable, and so do the bookings that reference it.</summary>
    Cancelled = 1,
}
