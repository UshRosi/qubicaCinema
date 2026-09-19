using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Application.Bookings.CreateBooking;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.UnitTests.Fixtures;

namespace QubicaCinema.Bookings.UnitTests.Handlers;

/// <summary>The bounded retry that turns a lost race for automatically chosen seats into a second choice.</summary>
public sealed class BookingPlacementTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly TestAuditorium _room = new(2, 4);
    private int _composed;

    [Fact]
    public async Task Should_commit_once_when_nothing_is_contended()
    {
        Booking placed = await Place(isWorthRetrying: _ => true);

        _composed.ShouldBe(1);
        _bookings.Received(1).Add(placed);
        _bookings.DidNotReceive().Discard(Arg.Any<Booking>());
    }

    [Fact]
    public async Task Should_compose_again_after_losing_a_race_that_is_worth_retrying()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(Conflict()), Task.FromResult(1));

        Booking placed = await Place(isWorthRetrying: _ => true);

        _composed.ShouldBe(2);
        _bookings.Received(1).Discard(Arg.Is<Booking>(discarded => discarded != placed));
        _bookings.Received(1).Add(placed);
    }

    [Fact]
    public async Task Should_report_the_conflict_straight_away_when_the_seats_were_named()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(Conflict()));

        await Should.ThrowAsync<SeatAlreadyBookedException>(Place(isWorthRetrying: _ => false));

        _composed.ShouldBe(1);
    }

    [Fact]
    public async Task Should_give_up_after_the_last_attempt()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(Conflict()));

        await Should.ThrowAsync<SeatAlreadyBookedException>(Place(isWorthRetrying: _ => true));

        _composed.ShouldBe(BookingPlacement.MaxAttempts);
    }

    private Task<Booking> Place(Func<SeatAlreadyBookedException, bool> isWorthRetrying) =>
        new BookingPlacement(_bookings, _unitOfWork).PlaceAsync(
            _ =>
            {
                _composed++;
                return Task.FromResult(Booking.Create(
                    Guid.CreateVersion7(), [_room.Claim(_room.Screening(Now.AddDays(1)), "A1")], _clock));
            },
            isWorthRetrying,
            TestContext.Current.CancellationToken);

    private SeatAlreadyBookedException Conflict() => new(Guid.CreateVersion7(), [_room["A1"].Id]);
}
