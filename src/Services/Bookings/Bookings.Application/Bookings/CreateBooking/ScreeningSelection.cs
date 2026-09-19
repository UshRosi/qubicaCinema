using QubicaCinema.Bookings.Domain.SeatAllocation;

namespace QubicaCinema.Bookings.Application.Bookings.CreateBooking;

/// <summary>The seats wanted at one screening of a booking.</summary>
/// <param name="ScreeningId">The screening.</param>
/// <param name="Selection">Which seats, or how many.</param>
public sealed record ScreeningSelection(Guid ScreeningId, SeatSelection Selection);
