namespace QubicaCinema.Bookings.Application.Screenings;

/// <summary>One seat of the map, and whether it can be booked.</summary>
/// <param name="SeatId">The seat, as it is named in a booking request.</param>
/// <param name="Row">Its row.</param>
/// <param name="Number">Its number within the row.</param>
/// <param name="IsAvailable">Whether nobody holds it for this screening.</param>
public sealed record SeatStateView(Guid SeatId, string Row, int Number, bool IsAvailable);
