using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.UnitTests.Fixtures;

namespace QubicaCinema.Bookings.UnitTests.Screenings;

public sealed class ScreeningTests
{
    private static readonly DateTimeOffset StartsAt = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    private readonly TestAuditorium _room = new(1, 1);

    [Fact]
    public void Should_be_open_for_booking_until_it_starts() =>
        Should.NotThrow(() => _room.Screening(StartsAt).EnsureOpenForBooking(StartsAt.AddTicks(-1)));

    [Fact]
    public void Should_refuse_bookings_from_the_moment_it_starts() =>
        Should.Throw<ScreeningStartedException>(() => _room.Screening(StartsAt).EnsureOpenForBooking(StartsAt));
}
