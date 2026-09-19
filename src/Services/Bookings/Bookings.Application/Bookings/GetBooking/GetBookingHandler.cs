using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Bookings.Application.Abstractions.Queries;
using QubicaCinema.Bookings.Domain.Exceptions;

namespace QubicaCinema.Bookings.Application.Bookings.GetBooking;

/// <inheritdoc cref="GetBookingQuery" />
public sealed class GetBookingHandler(IBookingQueries queries, ICurrentUser currentUser)
    : IQueryHandler<GetBookingQuery, Versioned<BookingView>>
{
    /// <inheritdoc />
    /// <exception cref="BookingNotFoundException">No such booking, or it belongs to someone else.</exception>
    public async Task<Versioned<BookingView>> HandleAsync(GetBookingQuery query, CancellationToken cancellationToken)
    {
        Versioned<BookingView>? booking = await queries.FindAsync(query.BookingId, cancellationToken);

        return booking is not null && currentUser.MayAccess(booking.Value.UserId)
            ? booking
            : throw new BookingNotFoundException(query.BookingId);
    }
}
