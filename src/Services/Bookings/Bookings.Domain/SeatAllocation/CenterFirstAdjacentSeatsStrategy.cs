using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Domain.ValueObjects;

namespace QubicaCinema.Bookings.Domain.SeatAllocation;

/// <summary>
/// Seats a party together, as close to the middle of the auditorium as possible.
/// </summary>
/// <remarks>
/// Every run of free seats side by side that is long enough is a candidate. The best one is in the row
/// nearest the middle of the room; between two rows equally near, the one further back, because people
/// would rather sit further from the screen than closer to it; within a row, the run whose centre is
/// nearest the centre of the row. The last tie-break is the seat order itself, so the same map always
/// gives the same answer — which is what makes the strategy testable, and a customer's retry predictable.
/// <para>
/// A pure function over the map: no state, no clock, no I/O. That is why it is registered as a singleton
/// and tested directly, with no mocks.
/// </para>
/// </remarks>
public sealed class CenterFirstAdjacentSeatsStrategy : ISeatAllocationStrategy
{
    /// <inheritdoc />
    public IReadOnlyList<Seat> Allocate(SeatMap seatMap, SeatCount count)
    {
        List<IGrouping<string, Seat>> rows = [.. seatMap.Seats.GroupBy(seat => seat.Position.Row)];
        double middleRow = (rows.Count - 1) / 2.0;

        Candidate? best = rows
            .SelectMany((row, rowIndex) => RunsOfFreeSeats(row, seatMap, count.Value)
                .Select(run => new Candidate(
                    run,
                    RowIndex: rowIndex,
                    RowDistance: Math.Abs(rowIndex - middleRow),
                    SeatDistance: Math.Abs(CentreOf(run) - CentreOf(row)))))
            .OrderBy(candidate => candidate.RowDistance)
            .ThenByDescending(candidate => candidate.RowIndex)
            .ThenBy(candidate => candidate.SeatDistance)
            .ThenBy(candidate => candidate.Seats[0].Position.Number)
            .FirstOrDefault();

        return best?.Seats
               ?? throw new NotEnoughAdjacentSeatsException(seatMap.Screening.Id, count.Value, seatMap.AvailableCount);
    }

    /// <summary>
    /// Every window of <paramref name="length"/> consecutive seats in one row that are all free. Windows
    /// overlap on purpose: in a free row of ten, a party of two can sit in nine places, and the scoring
    /// decides which of them.
    /// </summary>
    private static IEnumerable<IReadOnlyList<Seat>> RunsOfFreeSeats(IEnumerable<Seat> row, SeatMap seatMap, int length)
    {
        Seat[] seats = [.. row.OrderBy(seat => seat.Position.Number)];

        for (int start = 0; start + length <= seats.Length; start++)
        {
            ArraySegment<Seat> window = new(seats, start, length);

            if (window.All(seatMap.IsAvailable) && IsContiguous(window))
            {
                yield return window.ToArray();
            }
        }
    }

    /// <summary>Consecutive numbers, not merely consecutive in the list: a missing seat breaks the run.</summary>
    private static bool IsContiguous(IReadOnlyList<Seat> seats)
    {
        for (int index = 1; index < seats.Count; index++)
        {
            if (!seats[index].Position.IsNextTo(seats[index - 1].Position))
            {
                return false;
            }
        }

        return true;
    }

    private static double CentreOf(IEnumerable<Seat> seats)
    {
        int[] numbers = [.. seats.Select(seat => seat.Position.Number)];

        return (numbers.Min() + numbers.Max()) / 2.0;
    }

    /// <summary>One run of seats and how good it is. Private: it exists only to be ranked.</summary>
    private sealed record Candidate(IReadOnlyList<Seat> Seats, int RowIndex, double RowDistance, double SeatDistance);
}
