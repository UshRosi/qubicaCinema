namespace QubicaCinema.Bookings.Domain.Bookings;

/// <summary>Whether a booking still holds any seat.</summary>
/// <remarks>
/// There is no <c>Deleted</c>. A cancelled booking is kept and stays readable: it is the customer's record
/// of what they bought and gave back, and the seats it once held must not look as if nobody ever had them.
/// </remarks>
public enum BookingStatus
{
    /// <summary>At least one of its items still holds a seat.</summary>
    Confirmed = 0,

    /// <summary>Every item was given back.</summary>
    Cancelled = 1,
}
