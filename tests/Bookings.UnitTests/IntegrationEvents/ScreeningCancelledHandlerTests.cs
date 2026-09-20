using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Application.IntegrationEvents;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.UnitTests.Fixtures;

namespace QubicaCinema.Bookings.UnitTests.IntegrationEvents;

public sealed class ScreeningCancelledHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IScreeningRepository _screenings = Substitute.For<IScreeningRepository>();
    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly TestAuditorium _room = new(2, 4);
    private readonly Screening _cancelled;
    private readonly Screening _other;
    private readonly ScreeningCancelledHandler _handler;

    public ScreeningCancelledHandlerTests()
    {
        _cancelled = _room.Screening(Now.AddDays(1));
        _other = _room.Screening(Now.AddDays(2));
        _handler = new ScreeningCancelledHandler(_screenings, _bookings, _clock);

        _screenings.FindAsync(_cancelled.Id, Arg.Any<CancellationToken>()).Returns(_cancelled);
    }

    [Fact]
    public async Task Should_mark_the_screening_cancelled()
    {
        _bookings.GetHoldingSeatsForAsync(_cancelled.Id, Arg.Any<CancellationToken>()).Returns([]);

        await Handle();

        _cancelled.Status.ShouldBe(ScreeningStatus.Cancelled);
    }

    [Fact]
    public async Task Should_give_back_the_seats_of_every_affected_booking()
    {
        var onlyThisOne = Booking.Create(Guid.CreateVersion7(), [_room.Claim(_cancelled, "A1", "A2")], _clock);
        var thisAndAnother = Booking.Create(
            Guid.CreateVersion7(), [_room.Claim(_cancelled, "B1"), _room.Claim(_other, "B1")], _clock);
        _bookings.GetHoldingSeatsForAsync(_cancelled.Id, Arg.Any<CancellationToken>()).Returns([onlyThisOne, thisAndAnother]);

        await Handle();

        onlyThisOne.Status.ShouldBe(BookingStatus.Cancelled);
        thisAndAnother.Status.ShouldBe(BookingStatus.Confirmed);
        thisAndAnother.ActiveItems.ShouldHaveSingleItem().ScreeningId.ShouldBe(_other.Id);
    }

    [Fact]
    public async Task Should_fail_for_an_unknown_screening_so_the_broker_redelivers_it()
    {
        var unknown = new ScreeningCancelled(Guid.CreateVersion7()) { OccurredAt = Now };
        _screenings.FindAsync(unknown.ScreeningId, Arg.Any<CancellationToken>()).Returns((Screening?)null);

        await Should.ThrowAsync<ScreeningNotFoundException>(() => _handler.HandleAsync(unknown, CancellationToken.None));
    }

    private Task Handle() =>
        _handler.HandleAsync(new ScreeningCancelled(_cancelled.Id, "Projector failure") { OccurredAt = Now }, CancellationToken.None);
}
