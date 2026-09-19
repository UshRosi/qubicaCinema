namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>The seats wanted at one screening.</summary>
/// <param name="ScreeningId">The screening.</param>
/// <param name="Selection">Which seats, or how many.</param>
public sealed record BookingItemRequest(Guid ScreeningId, SeatSelectionRequest Selection);
