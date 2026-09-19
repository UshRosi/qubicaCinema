using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Bookings.Application.Abstractions.Queries;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Application.Bookings;
using QubicaCinema.Bookings.Application.Bookings.CancelBooking;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.UnitTests.Fixtures;

namespace QubicaCinema.Bookings.UnitTests.Handlers;

/// <summary>Ownership is decided here, with the loaded booking, and a stranger is told the booking does not exist.</summary>
public sealed class CancelBookingHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly IScreeningRepository _screenings = Substitute.For<IScreeningRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IBookingQueries _queries = Substitute.For<IBookingQueries>();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly StubCurrentUser _owner = StubCurrentUser.Customer();
    private readonly Booking _booking;

    public CancelBookingHandlerTests()
    {
        var room = new TestAuditorium(2, 4);
        Screening tonight = room.Screening(Now.AddHours(8));
        _booking = Booking.Create(_owner.UserId, [room.Claim(tonight, "A1")], _clock);

        _bookings.FindAsync(_booking.Id, Arg.Any<CancellationToken>()).Returns(_booking);
        _screenings.GetManyAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([tonight]);
        _queries.FindAsync(_booking.Id, Arg.Any<CancellationToken>()).Returns(new Versioned<BookingView>(
            new BookingView(_booking.Id, _owner.UserId, BookingStatus.Cancelled, Now, Now, 0m, "EUR", []),
            new byte[8]));
    }

    [Fact]
    public async Task Should_cancel_the_owners_booking_and_commit()
    {
        await Handle(_owner);

        _booking.Status.ShouldBe(BookingStatus.Cancelled);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_let_an_administrator_cancel_on_the_customers_behalf()
    {
        await Handle(StubCurrentUser.Administrator());

        _booking.Status.ShouldBe(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Should_tell_a_stranger_the_booking_does_not_exist()
    {
        await Should.ThrowAsync<BookingNotFoundException>(Handle(StubCurrentUser.Customer()));

        _booking.Status.ShouldBe(BookingStatus.Confirmed);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_report_an_unknown_booking_as_not_found()
    {
        _bookings.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Booking?)null);

        await Should.ThrowAsync<BookingNotFoundException>(Handle(_owner));
    }

    private Task<Versioned<BookingView>> Handle(StubCurrentUser caller) =>
        new CancelBookingHandler(_bookings, _screenings, _unitOfWork, _queries, caller, _clock)
            .HandleAsync(new CancelBookingCommand(_booking.Id), TestContext.Current.CancellationToken);
}
