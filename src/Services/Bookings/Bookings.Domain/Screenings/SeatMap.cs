using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.SeatAllocation;

namespace QubicaCinema.Bookings.Domain.Screenings;

/// <summary>
/// Every seat of one screening's auditorium, and which of them are already booked for that screening.
/// </summary>
/// <remarks>
/// A snapshot, not a hold. It says what was free when it was read; two customers can read the same map
/// and pick the same seat. The model uses it to give a clear answer in the common case, and the filtered
/// unique index in the database is what settles the race — see <see cref="SeatAlreadyBookedException"/>.
/// <para>
/// This is also the one definition of "available": the availability endpoint and the booking use case both
/// ask this type, so they cannot disagree about which seats are free.
/// </para>
/// </remarks>
public sealed class SeatMap
{
    private readonly HashSet<Guid> _bookedSeatIds;
    private readonly Dictionary<Guid, Seat> _seatsById;

    /// <summary>Builds the map of a screening.</summary>
    /// <param name="screening">The screening.</param>
    /// <param name="seats">Every seat in its auditorium.</param>
    /// <param name="bookedSeatIds">The seats held by an active booking item for this screening.</param>
    /// <exception cref="ArgumentException">A seat belongs to another auditorium.</exception>
    public SeatMap(Screening screening, IEnumerable<Seat> seats, IEnumerable<Guid> bookedSeatIds)
    {
        Screening = screening;
        Seats = [.. seats.OrderBy(seat => seat.Position.Row, StringComparer.Ordinal).ThenBy(seat => seat.Position.Number)];

        if (Seats.FirstOrDefault(seat => seat.AuditoriumId != screening.AuditoriumId) is { } stray)
        {
            throw new ArgumentException(
                $"Seat {stray.Id} is in auditorium {stray.AuditoriumId}, not in {screening.AuditoriumId}.",
                nameof(seats));
        }

        _seatsById = Seats.ToDictionary(seat => seat.Id);
        _bookedSeatIds = [.. bookedSeatIds];
    }

    /// <summary>The screening this map is for.</summary>
    public Screening Screening { get; }

    /// <summary>Every seat in the auditorium, ordered by row and then by number.</summary>
    public IReadOnlyList<Seat> Seats { get; }

    /// <summary>The seats nobody holds, in the same order.</summary>
    public IEnumerable<Seat> AvailableSeats => Seats.Where(IsAvailable);

    /// <summary>How many seats are free.</summary>
    public int AvailableCount => Seats.Count - BookedCount;

    /// <summary>How many seats are held by an active booking.</summary>
    public int BookedCount => Seats.Count(seat => _bookedSeatIds.Contains(seat.Id));

    /// <summary>Whether nobody holds the seat for this screening.</summary>
    public bool IsAvailable(Seat seat) => !_bookedSeatIds.Contains(seat.Id);

    /// <summary>
    /// Turns what the customer asked for into concrete seats: the ones named, or as many as asked for,
    /// chosen by the strategy.
    /// </summary>
    /// <exception cref="SeatNotFoundException">A named seat is not in this auditorium.</exception>
    /// <exception cref="SeatAlreadyBookedException">A named seat is already booked.</exception>
    /// <exception cref="NotEnoughAdjacentSeatsException">No row has enough free seats side by side.</exception>
    public SeatClaim Claim(SeatSelection selection, ISeatAllocationStrategy strategy) => selection switch
    {
        SeatSelection.Explicit chosen => new SeatClaim(Screening, ClaimNamed(chosen.SeatIds)),
        SeatSelection.ByQuantity quantity => new SeatClaim(Screening, strategy.Allocate(this, quantity.Count)),

        // SeatSelection is a closed union, so this arm is unreachable; the compiler cannot prove it.
        _ => throw new ArgumentOutOfRangeException(nameof(selection), selection, "Unknown kind of seat selection."),
    };

    private List<Seat> ClaimNamed(IReadOnlyCollection<Guid> seatIds)
    {
        Guid[] unknown = [.. seatIds.Where(id => !_seatsById.ContainsKey(id))];

        if (unknown.Length > 0)
        {
            throw new SeatNotFoundException(Screening.Id, unknown);
        }

        Guid[] taken = [.. seatIds.Where(_bookedSeatIds.Contains)];

        if (taken.Length > 0)
        {
            throw new SeatAlreadyBookedException(Screening.Id, taken);
        }

        return [.. seatIds.Select(id => _seatsById[id])];
    }
}
