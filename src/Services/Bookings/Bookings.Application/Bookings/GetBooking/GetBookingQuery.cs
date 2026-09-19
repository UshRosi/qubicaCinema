namespace QubicaCinema.Bookings.Application.Bookings.GetBooking;

/// <summary>Asks for one booking.</summary>
/// <param name="BookingId">Which booking.</param>
public sealed record GetBookingQuery(Guid BookingId);
