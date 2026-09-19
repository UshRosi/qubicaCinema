using NSubstitute;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Bookings.Application.Abstractions.Queries;
using QubicaCinema.Bookings.Application.Bookings;
using QubicaCinema.Bookings.Application.Bookings.GetBookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.UnitTests.Fixtures;

namespace QubicaCinema.Bookings.UnitTests.Handlers;

/// <summary>Whose bookings a caller may list.</summary>
public sealed class GetBookingsHandlerTests
{
    private readonly IBookingQueries _queries = Substitute.For<IBookingQueries>();

    [Fact]
    public async Task Should_list_a_customers_own_bookings_when_no_user_is_named()
    {
        var customer = StubCurrentUser.Customer();

        await Handle(customer, userId: null);

        await _queries.Received(1).GetPageAsync(customer.UserId, 0, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_forbid_a_customer_from_listing_someone_elses_bookings() =>
        await Should.ThrowAsync<BookingListForbiddenException>(Handle(StubCurrentUser.Customer(), Guid.CreateVersion7()));

    [Fact]
    public async Task Should_let_an_administrator_list_everyones_bookings()
    {
        await Handle(StubCurrentUser.Administrator(), userId: null);

        await _queries.Received(1).GetPageAsync(null, 0, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_let_an_administrator_list_one_customers_bookings()
    {
        var someone = Guid.CreateVersion7();

        await Handle(StubCurrentUser.Administrator(), someone);

        await _queries.Received(1).GetPageAsync(someone, 0, 20, Arg.Any<CancellationToken>());
    }

    private Task<PagedResult<BookingView>> Handle(StubCurrentUser caller, Guid? userId) =>
        new GetBookingsHandler(_queries, caller)
            .HandleAsync(new GetBookingsQuery(userId, Skip: 0, Take: 20), TestContext.Current.CancellationToken);
}
