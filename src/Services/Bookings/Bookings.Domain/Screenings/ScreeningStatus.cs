namespace QubicaCinema.Bookings.Domain.Screenings;

/// <summary>Whether a screening is still going to happen, as Catalog last announced it.</summary>
public enum ScreeningStatus
{
    /// <summary>It is going to happen, and its seats can be booked.</summary>
    Scheduled = 0,

    /// <summary>It was called off. Bookings for it stay readable; no new seat can be booked.</summary>
    Cancelled = 1,
}
