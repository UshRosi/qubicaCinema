using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;

namespace QubicaCinema.BuildingBlocks.UnitTests.ValueObjects;

public sealed class SeatPositionTests
{
    [Fact]
    public void Should_normalise_the_row_to_an_upper_case_letter()
    {
        SeatPosition.Of("c", 7).Row.ShouldBe("C");
    }

    [Fact]
    public void Should_render_itself_the_way_a_ticket_does()
    {
        SeatPosition.Of("C", 7).ToString().ShouldBe("C7");
    }

    [Theory]
    [InlineData("")]
    [InlineData("AA")]
    [InlineData("1")]
    public void Should_reject_a_row_that_is_not_a_single_letter(string row)
    {
        Should.Throw<InvalidSeatPositionException>(() => SeatPosition.Of(row, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(SeatPosition.MaxNumber + 1)]
    public void Should_reject_a_seat_number_out_of_range(int number)
    {
        Should.Throw<InvalidSeatPositionException>(() => SeatPosition.Of("A", number));
    }

    [Fact]
    public void Should_be_equal_to_the_same_position_in_another_auditorium()
    {
        // Structural equality on purpose: identity is the seat's id, not where it sits.
        SeatPosition.Of("A", 1).ShouldBe(SeatPosition.Of("a", 1));
    }
}
