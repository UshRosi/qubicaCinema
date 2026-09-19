using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Domain.SeatAllocation;
using QubicaCinema.Bookings.Domain.ValueObjects;
using QubicaCinema.Bookings.UnitTests.Fixtures;

namespace QubicaCinema.Bookings.UnitTests.SeatAllocation;

/// <summary>A pure function over a seat map, so these tests need no mocks at all.</summary>
public sealed class CenterFirstAdjacentSeatsStrategyTests
{
    private static readonly DateTimeOffset Tonight = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    private readonly CenterFirstAdjacentSeatsStrategy _strategy = new();

    [Fact]
    public void Should_seat_a_party_in_the_middle_of_the_middle_row()
    {
        var room = new TestAuditorium(rowCount: 5, seatsPerRow: 10);

        Allocate(room, 2).ShouldBe(["C5", "C6"]);
    }

    [Fact]
    public void Should_break_a_tie_between_equally_central_runs_towards_the_lower_seat_numbers()
    {
        var room = new TestAuditorium(rowCount: 5, seatsPerRow: 10);

        // C4-C6 and C5-C7 are both half a seat off centre; the choice must still be the same every time.
        Allocate(room, 3).ShouldBe(["C4", "C5", "C6"]);
    }

    [Fact]
    public void Should_prefer_the_row_further_back_when_two_rows_are_equally_central()
    {
        var room = new TestAuditorium(rowCount: 4, seatsPerRow: 6);

        Allocate(room, 2).ShouldBe(["C3", "C4"]);
    }

    [Fact]
    public void Should_keep_the_party_together_around_a_taken_seat()
    {
        var room = new TestAuditorium(rowCount: 5, seatsPerRow: 10);

        Allocate(room, 2, booked: "C5").ShouldBe(["C6", "C7"]);
    }

    [Fact]
    public void Should_move_to_the_next_most_central_row_when_the_middle_one_is_full()
    {
        var room = new TestAuditorium(rowCount: 5, seatsPerRow: 4);
        string[] middleRow = ["C1", "C2", "C3", "C4"];

        Allocate(room, 2, middleRow).ShouldBe(["D2", "D3"]);
    }

    [Fact]
    public void Should_refuse_to_split_a_party_across_scattered_seats()
    {
        var room = new TestAuditorium(rowCount: 1, seatsPerRow: 4);

        var refusal = Should.Throw<NotEnoughAdjacentSeatsException>(() => Allocate(room, 3, booked: "A2"));

        refusal.Extensions["available"].ShouldBe(3);
    }

    [Fact]
    public void Should_say_so_when_there_are_simply_not_enough_seats()
    {
        var room = new TestAuditorium(rowCount: 1, seatsPerRow: 4);

        var refusal = Should.Throw<NotEnoughAdjacentSeatsException>(() => Allocate(room, 5));

        refusal.Message.ShouldContain("fewer than the 5 requested");
    }

    private string[] Allocate(TestAuditorium room, int count, params string[] booked)
    {
        SeatMap map = room.MapOf(room.Screening(Tonight), booked);

        return [.. _strategy.Allocate(map, SeatCount.Of(count)).Select(seat => seat.ToString())];
    }
}
