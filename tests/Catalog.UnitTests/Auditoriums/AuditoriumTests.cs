using QubicaCinema.Catalog.Domain.Auditoriums;
using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.ValueObjects;

namespace QubicaCinema.Catalog.UnitTests.Auditoriums;

public sealed class AuditoriumTests
{
    [Fact]
    public void Should_build_the_whole_seat_grid()
    {
        var auditorium = Auditorium.Create("Sala Rossa", rowCount: 3, seatsPerRow: 4);

        auditorium.Seats.Count.ShouldBe(12);
        auditorium.Capacity.ShouldBe(12);
        auditorium.Seats.Select(seat => seat.Position.ToString())
            .ShouldBe(["A1", "A2", "A3", "A4", "B1", "B2", "B3", "B4", "C1", "C2", "C3", "C4"], ignoreOrder: true);
    }

    [Fact]
    public void Should_give_every_seat_its_own_identity_and_its_auditorium()
    {
        var auditorium = Auditorium.Create("Sala Rossa", rowCount: 2, seatsPerRow: 2);

        auditorium.Seats.Select(seat => seat.Id).Distinct().Count().ShouldBe(4);
        auditorium.Seats.ShouldAllBe(seat => seat.AuditoriumId == auditorium.Id);
    }

    [Fact]
    public void Should_trim_the_name()
    {
        Auditorium.Create("  Sala Rossa  ", 1, 1).Name.ShouldBe("Sala Rossa");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_a_missing_name(string name)
    {
        Should.Throw<InvalidAuditoriumLayoutException>(() => Auditorium.Create(name, 1, 1));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(SeatPosition.MaxRows + 1, 10)]
    [InlineData(10, 0)]
    [InlineData(10, SeatPosition.MaxNumber + 1)]
    public void Should_reject_a_grid_it_cannot_build(int rowCount, int seatsPerRow)
    {
        Should.Throw<InvalidAuditoriumLayoutException>(
            () => Auditorium.Create("Sala Rossa", rowCount, seatsPerRow));
    }

    [Fact]
    public void Should_find_a_seat_by_its_position()
    {
        var auditorium = Auditorium.Create("Sala Rossa", 3, 4);

        auditorium.FindSeat(SeatPosition.Of("B", 2)).ShouldNotBeNull();
        auditorium.FindSeat(SeatPosition.Of("D", 1)).ShouldBeNull();
    }

    [Fact]
    public void Should_expose_its_seats_read_only()
    {
        // The collection type itself is the guarantee: there is no way to add a duplicate A1 from outside.
        Auditorium.Create("Sala Rossa", 1, 1).Seats.ShouldBeAssignableTo<IReadOnlyCollection<Seat>>();
    }
}
