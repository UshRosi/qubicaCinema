using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Bookings.Application.Abstractions.Queries;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Application.Bookings;
using QubicaCinema.Bookings.Application.Bookings.CreateBooking;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Domain.SeatAllocation;
using QubicaCinema.Bookings.Domain.ValueObjects;
using QubicaCinema.Bookings.UnitTests.Fixtures;

namespace QubicaCinema.Bookings.UnitTests.Handlers;

/// <summary>
/// The handler's own job: read each seat map, let the domain resolve the selection, place the booking,
/// and read it back. The rules themselves are tested on the domain types.
/// </summary>
public sealed class CreateBookingHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ISeatMapRepository _seatMaps = Substitute.For<ISeatMapRepository>();
    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IBookingQueries _queries = Substitute.For<IBookingQueries>();
    private readonly StubCurrentUser _customer = StubCurrentUser.Customer();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly TestAuditorium _room = new(rowCount: 3, seatsPerRow: 6);
    private readonly Screening _tonight;

    public CreateBookingHandlerTests()
    {
        _tonight = _room.Screening(Now.AddHours(8));
        _queries.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => new Versioned<BookingView>(ViewOf(call.Arg<Guid>()), new byte[8]));
    }

    [Fact]
    public async Task Should_book_the_named_seats_for_the_current_user()
    {
        GivenSeatMap(_room.MapOf(_tonight));

        await Handle(new SeatSelection.Explicit([_room["B1"].Id, _room["B2"].Id]));

        _bookings.Received(1).Add(Arg.Is<Booking>(booking =>
            booking.UserId == _customer.UserId
            && booking.Items.Select(item => item.SeatId).SequenceEqual(new[] { _room["B1"].Id, _room["B2"].Id })));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_answer_with_the_booking_as_it_was_committed()
    {
        GivenSeatMap(_room.MapOf(_tonight));
        Booking? added = null;
        _bookings.Add(Arg.Do<Booking>(booking => added = booking));

        Versioned<BookingView> result = await Handle(new SeatSelection.ByQuantity(SeatCount.Of(2)));

        result.Value.Id.ShouldBe(added!.Id);
    }

    [Fact]
    public async Task Should_report_an_unknown_screening_as_not_found()
    {
        _seatMaps.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SeatMap?)null);

        await Should.ThrowAsync<ScreeningNotFoundException>(Handle(new SeatSelection.ByQuantity(SeatCount.Of(1))));

        _bookings.DidNotReceive().Add(Arg.Any<Booking>());
    }

    [Fact]
    public async Task Should_read_the_seat_map_again_and_choose_other_seats_after_losing_a_race()
    {
        // First read: the map looks empty. The commit loses to a booking that took the centre pair, and
        // the second read shows it taken, so the strategy picks the next best pair.
        _seatMaps.FindAsync(_tonight.Id, Arg.Any<CancellationToken>())
            .Returns(_room.MapOf(_tonight), _room.MapOf(_tonight, "B3", "B4"));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new SeatAlreadyBookedException(_tonight.Id, [_room["B3"].Id])), Task.FromResult(1));

        await Handle(new SeatSelection.ByQuantity(SeatCount.Of(2)));

        await _seatMaps.Received(2).FindAsync(_tonight.Id, Arg.Any<CancellationToken>());
        _bookings.Received(1).Discard(Arg.Any<Booking>());
    }

    [Fact]
    public async Task Should_not_swap_seats_the_customer_named_for_others()
    {
        GivenSeatMap(_room.MapOf(_tonight));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new SeatAlreadyBookedException(_tonight.Id, [_room["B1"].Id])));

        await Should.ThrowAsync<SeatAlreadyBookedException>(Handle(new SeatSelection.Explicit([_room["B1"].Id])));

        await _seatMaps.Received(1).FindAsync(_tonight.Id, Arg.Any<CancellationToken>());
    }

    private void GivenSeatMap(SeatMap map) =>
        _seatMaps.FindAsync(map.Screening.Id, Arg.Any<CancellationToken>()).Returns(map);

    private Task<Versioned<BookingView>> Handle(SeatSelection selection) =>
        new CreateBookingHandler(
                _seatMaps,
                new CenterFirstAdjacentSeatsStrategy(),
                new BookingPlacement(_bookings, _unitOfWork),
                _queries,
                _customer,
                _clock)
            .HandleAsync(
                new CreateBookingCommand([new ScreeningSelection(_tonight.Id, selection)]),
                TestContext.Current.CancellationToken);

    private BookingView ViewOf(Guid bookingId) =>
        new(bookingId, _customer.UserId, BookingStatus.Confirmed, Now, null, 0m, "EUR", []);
}
