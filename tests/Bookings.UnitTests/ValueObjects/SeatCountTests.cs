using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.ValueObjects;

namespace QubicaCinema.Bookings.UnitTests.ValueObjects;

public sealed class SeatCountTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void Should_accept_a_count_within_bounds(int value) => SeatCount.Of(value).Value.ShouldBe(value);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void Should_reject_a_count_out_of_bounds(int value) =>
        Should.Throw<InvalidSeatCountException>(() => SeatCount.Of(value));

    [Fact]
    public void Should_compare_by_value() => SeatCount.Of(3).ShouldBe(SeatCount.Of(3));
}
