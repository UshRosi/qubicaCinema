using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Domain.SeatAllocation;
using QubicaCinema.Bookings.Domain.ValueObjects;
using QubicaCinema.Bookings.UnitTests.Fixtures;

namespace QubicaCinema.Bookings.UnitTests.Screenings;

public sealed class SeatMapTests
{
    private static readonly DateTimeOffset Tonight = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    private readonly TestAuditorium _room = new(rowCount: 3, seatsPerRow: 4);
    private readonly ISeatAllocationStrategy _strategy = new CenterFirstAdjacentSeatsStrategy();

    [Fact]
    public void Should_count_free_and_booked_seats()
    {
        SeatMap map = _room.MapOf(_room.Screening(Tonight), "A1", "B2");

        map.Seats.Count.ShouldBe(12);
        map.BookedCount.ShouldBe(2);
        map.AvailableCount.ShouldBe(10);
        map.IsAvailable(_room["A1"]).ShouldBeFalse();
        map.IsAvailable(_room["A2"]).ShouldBeTrue();
    }

    [Fact]
    public void Should_claim_the_named_seats_when_they_are_free()
    {
        SeatMap map = _room.MapOf(_room.Screening(Tonight));

        SeatClaim claim = map.Claim(new SeatSelection.Explicit([_room["B1"].Id, _room["B2"].Id]), _strategy);

        claim.Seats.ShouldBe([_room["B1"], _room["B2"]]);
        claim.Screening.ShouldBeSameAs(map.Screening);
    }

    [Fact]
    public void Should_name_exactly_the_seats_that_are_already_taken()
    {
        SeatMap map = _room.MapOf(_room.Screening(Tonight), "B2");

        SeatAlreadyBookedException conflict = Should.Throw<SeatAlreadyBookedException>(() =>
            map.Claim(new SeatSelection.Explicit([_room["B1"].Id, _room["B2"].Id]), _strategy));

        conflict.ScreeningId.ShouldBe(map.Screening.Id);
        conflict.UnavailableSeatIds.ShouldBe([_room["B2"].Id]);
    }

    [Fact]
    public void Should_reject_a_seat_from_another_auditorium()
    {
        SeatMap map = _room.MapOf(_room.Screening(Tonight));
        var elsewhere = new TestAuditorium(1, 1);

        Should.Throw<SeatNotFoundException>(() =>
            map.Claim(new SeatSelection.Explicit([elsewhere["A1"].Id]), _strategy));
    }

    [Fact]
    public void Should_leave_a_quantity_to_the_strategy()
    {
        SeatMap map = _room.MapOf(_room.Screening(Tonight));

        SeatClaim claim = map.Claim(new SeatSelection.ByQuantity(SeatCount.Of(2)), _strategy);

        claim.Seats.Count.ShouldBe(2);
        claim.Seats.ShouldAllBe(seat => map.IsAvailable(seat));
    }

    [Fact]
    public void Should_refuse_seats_from_another_auditorium_when_built() =>
        Should.Throw<ArgumentException>(() =>
            new SeatMap(_room.Screening(Tonight), new TestAuditorium(1, 2).Seats, []));
}
