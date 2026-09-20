using Microsoft.Extensions.Time.Testing;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.UnitTests.Fixtures;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.UnitTests.Bookings;

public sealed class BookingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Now);
    private readonly Guid _customer = Guid.CreateVersion7();
    private readonly TestAuditorium _room = new(rowCount: 3, seatsPerRow: 6);
    private readonly Screening _tonight;
    private readonly Screening _tomorrow;

    public BookingTests()
    {
        _tonight = _room.Screening(Now.AddHours(8), price: 9.50m);
        _tomorrow = _room.Screening(Now.AddDays(1), price: 7.00m);
    }

    [Fact]
    public void Should_hold_one_active_item_per_seat_across_screenings()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1", "B2"), _room.Claim(_tomorrow, "C3"));

        booking.UserId.ShouldBe(_customer);
        booking.Status.ShouldBe(BookingStatus.Confirmed);
        booking.CreatedAt.ShouldBe(Now);
        booking.Items.Count.ShouldBe(3);
        booking.Items.ShouldAllBe(item => item.IsActive);
        booking.ScreeningIds.ShouldBe([_tonight.Id, _tomorrow.Id], ignoreOrder: true);
    }

    [Fact]
    public void Should_snapshot_each_screenings_price_and_total_them()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1", "B2"), _room.Claim(_tomorrow, "C3"));

        booking.Items.Where(item => item.ScreeningId == _tonight.Id).ShouldAllBe(item => item.Price == Money.Of(9.50m, "EUR"));
        booking.Total.ShouldBe(Money.Of(26.00m, "EUR"));
    }

    [Fact]
    public void Should_refuse_a_booking_without_seats() =>
        Should.Throw<InvalidBookingException>(() => Book());

    [Fact]
    public void Should_refuse_the_same_seat_twice_at_one_screening() =>
        Should.Throw<InvalidBookingException>(() => Book(_room.Claim(_tonight, "B1"), _room.Claim(_tonight, "B1")));

    [Fact]
    public void Should_refuse_more_screenings_than_one_booking_may_span()
    {
        SeatClaim[] claims =
        [
            .. Enumerable.Range(0, Booking.MaxScreenings + 1)
                .Select(day => _room.Claim(_room.Screening(Now.AddDays(day + 1)), "A1")),
        ];

        Should.Throw<InvalidBookingException>(() => Book(claims));
    }

    [Fact]
    public void Should_refuse_a_seat_that_is_not_in_the_screenings_auditorium()
    {
        var elsewhere = new TestAuditorium(1, 1);

        Should.Throw<SeatNotFoundException>(() => Book(new SeatClaim(_tonight, [elsewhere["A1"]])));
    }

    [Fact]
    public void Should_refuse_a_screening_that_has_started() =>
        Should.Throw<ScreeningStartedException>(() => Book(_room.Claim(_room.Screening(Now.AddMinutes(-1)), "A1")));

    [Fact]
    public void Should_refuse_screenings_priced_in_different_currencies() =>
        Should.Throw<CurrencyMismatchException>(() =>
            Book(_room.Claim(_tonight, "A1"), _room.Claim(_room.Screening(Now.AddDays(2), currency: "USD"), "A1")));

    [Fact]
    public void Should_give_back_every_seat_and_stay_readable_when_cancelled()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1", "B2"), _room.Claim(_tomorrow, "C3"));
        _clock.Advance(TimeSpan.FromHours(1));

        booking.Cancel([_tonight, _tomorrow], _clock);

        booking.Status.ShouldBe(BookingStatus.Cancelled);
        booking.CancelledAt.ShouldBe(Now.AddHours(1));
        booking.Items.Count.ShouldBe(3);
        booking.Items.ShouldAllBe(item => item.Status == BookingItemStatus.Cancelled);
        booking.Total.ShouldBe(Money.Zero("EUR"));
    }

    [Fact]
    public void Should_treat_a_second_cancellation_as_a_no_op()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1"));
        booking.Cancel([_tonight], _clock);
        DateTimeOffset? firstCancellation = booking.CancelledAt;
        _clock.Advance(TimeSpan.FromMinutes(5));

        booking.Cancel([_tonight], _clock);

        booking.CancelledAt.ShouldBe(firstCancellation);
    }

    [Fact]
    public void Should_cancel_nothing_when_any_seat_is_for_a_screening_that_has_started()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1"), _room.Claim(_tomorrow, "C3"));
        _clock.SetUtcNow(_tonight.StartsAt);

        Should.Throw<BookingNotCancellableException>(() => booking.Cancel([_tonight, _tomorrow], _clock));

        booking.Status.ShouldBe(BookingStatus.Confirmed);
        booking.Items.ShouldAllBe(item => item.IsActive);
    }

    [Fact]
    public void Should_give_back_one_seat_and_leave_it_out_of_the_total()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1", "B2"));
        BookingItem first = booking.Items.First();

        booking.CancelItem(first.Id, [_tonight], _clock);

        first.Status.ShouldBe(BookingItemStatus.Cancelled);
        booking.Status.ShouldBe(BookingStatus.Confirmed);
        booking.ActiveItems.Count().ShouldBe(1);
        booking.Total.ShouldBe(Money.Of(9.50m, "EUR"));
    }

    [Fact]
    public void Should_cancel_the_booking_when_its_last_seat_is_given_back()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1", "B2"));

        foreach (BookingItem item in booking.Items.ToList())
        {
            booking.CancelItem(item.Id, [_tonight], _clock);
        }

        booking.Status.ShouldBe(BookingStatus.Cancelled);
        booking.CancelledAt.ShouldBe(Now);
    }

    [Fact]
    public void Should_treat_cancelling_a_cancelled_item_as_a_no_op()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1", "B2"));
        BookingItem first = booking.Items.First();
        booking.CancelItem(first.Id, [_tonight], _clock);
        _clock.Advance(TimeSpan.FromMinutes(5));

        booking.CancelItem(first.Id, [_tonight], _clock);

        first.CancelledAt.ShouldBe(Now);
    }

    [Fact]
    public void Should_refuse_to_give_back_a_seat_once_its_screening_has_started()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1"));
        _clock.SetUtcNow(_tonight.StartsAt.AddMinutes(10));

        Should.Throw<BookingNotCancellableException>(() =>
            booking.CancelItem(booking.Items.First().Id, [_tonight], _clock));
    }

    [Fact]
    public void Should_report_an_item_the_booking_does_not_have() =>
        Should.Throw<BookingItemNotFoundException>(() =>
            Book(_room.Claim(_tonight, "B1")).CancelItem(Guid.CreateVersion7(), [_tonight], _clock));

    [Fact]
    public void Should_know_its_owner()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1"));

        booking.IsOwnedBy(_customer).ShouldBeTrue();
        booking.IsOwnedBy(Guid.CreateVersion7()).ShouldBeFalse();
    }

    [Fact]
    public void Should_release_only_the_seats_of_the_cancelled_screening()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1", "B2"), _room.Claim(_tomorrow, "C3"));

        booking.ReleaseSeatsFor(_tonight.Id, _clock);

        booking.Items.Where(item => item.ScreeningId == _tonight.Id).ShouldAllBe(item => !item.IsActive);
        booking.Items.Where(item => item.ScreeningId == _tomorrow.Id).ShouldAllBe(item => item.IsActive);
        booking.Status.ShouldBe(BookingStatus.Confirmed);
        booking.Total.ShouldBe(Money.Of(7.00m, "EUR"));
    }

    [Fact]
    public void Should_cancel_the_booking_when_its_only_screening_is_called_off()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1", "B2"));

        booking.ReleaseSeatsFor(_tonight.Id, _clock);

        booking.Status.ShouldBe(BookingStatus.Cancelled);
        booking.CancelledAt.ShouldBe(Now);
    }

    [Fact]
    public void Should_release_seats_even_after_the_screening_time_has_passed()
    {
        // The cinema is calling it off, so there is no start time for the customer to be too late for.
        Booking booking = Book(_room.Claim(_tonight, "B1"));
        _clock.Advance(TimeSpan.FromHours(9));

        Should.NotThrow(() => booking.ReleaseSeatsFor(_tonight.Id, _clock));

        booking.Status.ShouldBe(BookingStatus.Cancelled);
    }

    [Fact]
    public void Should_leave_a_booking_alone_when_it_holds_nothing_at_that_screening()
    {
        Booking booking = Book(_room.Claim(_tomorrow, "C3"));

        booking.ReleaseSeatsFor(_tonight.Id, _clock);

        booking.Status.ShouldBe(BookingStatus.Confirmed);
        booking.ActiveItems.Count().ShouldBe(1);
    }

    [Fact]
    public void Should_release_seats_only_once_when_the_event_is_redelivered()
    {
        Booking booking = Book(_room.Claim(_tonight, "B1"), _room.Claim(_tomorrow, "C3"));
        booking.ReleaseSeatsFor(_tonight.Id, _clock);
        DateTimeOffset firstRelease = booking.Items.First(item => item.ScreeningId == _tonight.Id).CancelledAt!.Value;

        _clock.Advance(TimeSpan.FromMinutes(5));
        booking.ReleaseSeatsFor(_tonight.Id, _clock);

        booking.Items.First(item => item.ScreeningId == _tonight.Id).CancelledAt.ShouldBe(firstRelease);
    }

    private Booking Book(params SeatClaim[] claims) => Booking.Create(_customer, claims, _clock);
}
