using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.UnitTests.Fixtures;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;

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

    [Fact]
    public void Should_take_the_new_time_and_price_when_rescheduled()
    {
        Screening screening = _room.Screening(StartsAt, price: 9.50m);

        screening.Reschedule(StartsAt.AddHours(2), Money.Of(12m, "EUR"));

        screening.StartsAt.ShouldBe(StartsAt.AddHours(2));
        screening.Price.ShouldBe(Money.Of(12m, "EUR"));
    }

    [Fact]
    public void Should_stay_cancelled_when_a_reschedule_arrives_late()
    {
        Screening screening = _room.Screening(StartsAt);
        screening.Cancel();

        screening.Reschedule(StartsAt.AddHours(2), Money.Of(12m, "EUR"));

        screening.Status.ShouldBe(ScreeningStatus.Cancelled);
        screening.StartsAt.ShouldBe(StartsAt);
    }

    [Fact]
    public void Should_refuse_bookings_once_cancelled()
    {
        Screening screening = _room.Screening(StartsAt);

        screening.Cancel();

        Should.Throw<ScreeningCancelledException>(() => screening.EnsureOpenForBooking(StartsAt.AddDays(-1)));
    }

    [Fact]
    public void Should_be_cancelled_only_once()
    {
        Screening screening = _room.Screening(StartsAt);

        screening.Cancel();
        screening.Cancel();

        screening.Status.ShouldBe(ScreeningStatus.Cancelled);
    }
}
