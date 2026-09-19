namespace QubicaCinema.Bookings.Application.Bookings.CancelBookingItem;

/// <summary>Gives back one seat of a booking.</summary>
/// <param name="BookingId">Which booking.</param>
/// <param name="ItemId">Which of its seats.</param>
public sealed record CancelBookingItemCommand(Guid BookingId, Guid ItemId);
