namespace QubicaCinema.Bookings.Domain.Screenings;

/// <summary>
/// The seats chosen for one screening, ready to become the items of a booking.
/// </summary>
/// <remarks>
/// What <see cref="SeatMap.Claim"/> hands to <see cref="Bookings.Booking.Create"/>: the screening, whose
/// state and price the booking needs, and the seats, already checked against the map as it stood.
/// </remarks>
/// <param name="Screening">The screening the seats are for.</param>
/// <param name="Seats">The seats, in the order they sit.</param>
public sealed record SeatClaim(Screening Screening, IReadOnlyList<Seat> Seats);
