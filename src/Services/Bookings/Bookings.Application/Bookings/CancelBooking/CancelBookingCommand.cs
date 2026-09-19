namespace QubicaCinema.Bookings.Application.Bookings.CancelBooking;

/// <summary>Gives back every seat of a booking.</summary>
/// <param name="BookingId">Which booking.</param>
public sealed record CancelBookingCommand(Guid BookingId);
