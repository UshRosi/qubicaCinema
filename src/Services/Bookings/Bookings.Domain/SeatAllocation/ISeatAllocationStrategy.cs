using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Domain.ValueObjects;

namespace QubicaCinema.Bookings.Domain.SeatAllocation;

/// <summary>
/// Chooses seats for a customer who asked for a number of seats rather than for particular ones.
/// </summary>
/// <remarks>
/// A strategy because "which seats are best?" is a policy a cinema may reasonably want to change — rear
/// rows first, keeping a gap between parties, accessibility seats last — while the booking rules around it
/// stay the same. A new policy is a new class; nothing that calls this interface changes.
/// </remarks>
public interface ISeatAllocationStrategy
{
    /// <summary>Picks <paramref name="count"/> free seats from the map.</summary>
    /// <returns>The chosen seats, in the order they sit.</returns>
    /// <exception cref="NotEnoughAdjacentSeatsException">The policy cannot find suitable seats.</exception>
    IReadOnlyList<Seat> Allocate(SeatMap seatMap, SeatCount count);
}
