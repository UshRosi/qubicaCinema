using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;

namespace QubicaCinema.Bookings.UnitTests.Fixtures;

/// <summary>
/// A rectangular auditorium for tests, with seats addressed the way a ticket prints them: <c>room["C5"]</c>.
/// </summary>
internal sealed class TestAuditorium
{
    public TestAuditorium(int rowCount, int seatsPerRow)
    {
        Seats =
        [
            .. from row in Enumerable.Range(0, rowCount)
               from number in Enumerable.Range(1, seatsPerRow)
               select Seat.Create(Guid.CreateVersion7(), Id, SeatPosition.Of(((char)('A' + row)).ToString(), number)),
        ];
    }

    public Guid Id { get; } = Guid.CreateVersion7();

    public IReadOnlyList<Seat> Seats { get; }

    public Seat this[string position] => Seats.Single(seat => seat.ToString() == position);

    public Screening Screening(DateTimeOffset startsAt, decimal price = 9.50m, string currency = "EUR") =>
        Domain.Screenings.Screening.Create(
            Guid.CreateVersion7(), Id, "Sala Prova", "Test Movie", startsAt, Money.Of(price, currency));

    public SeatMap MapOf(Screening screening, params string[] booked) =>
        new(screening, Seats, booked.Select(position => this[position].Id));

    public SeatClaim Claim(Screening screening, params string[] positions) =>
        new(screening, [.. positions.Select(position => this[position])]);
}
