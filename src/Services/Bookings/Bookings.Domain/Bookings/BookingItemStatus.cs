namespace QubicaCinema.Bookings.Domain.Bookings;

/// <summary>Whether one booked seat is still held.</summary>
/// <remarks>
/// The unique index that prevents double booking is filtered on <see cref="Active"/>: a cancelled item
/// keeps its row, and the seat becomes bookable again because the index no longer sees it.
/// </remarks>
public enum BookingItemStatus
{
    /// <summary>The seat is held by this booking.</summary>
    Active = 0,

    /// <summary>The seat was given back and is free for others.</summary>
    Cancelled = 1,
}
