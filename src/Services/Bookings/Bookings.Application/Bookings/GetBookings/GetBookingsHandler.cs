using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Bookings.Application.Abstractions.Queries;
using QubicaCinema.Bookings.Domain.Exceptions;

namespace QubicaCinema.Bookings.Application.Bookings.GetBookings;

/// <inheritdoc cref="GetBookingsQuery" />
public sealed class GetBookingsHandler(IBookingQueries queries, ICurrentUser currentUser)
    : IQueryHandler<GetBookingsQuery, PagedResult<BookingView>>
{
    /// <inheritdoc />
    /// <exception cref="BookingListForbiddenException">A customer asked for another user's bookings.</exception>
    /// <remarks>
    /// Async even though it only forwards: the refusal must arrive inside the returned task, as it would
    /// from any other asynchronous method, not be thrown before the caller has a task to await.
    /// </remarks>
    public async Task<PagedResult<BookingView>> HandleAsync(GetBookingsQuery query, CancellationToken cancellationToken) =>
        await queries.GetPageAsync(OwnerToList(query.UserId), query.Skip, query.Take, cancellationToken);

    /// <summary>Whose bookings the caller may list: anyone's for an administrator, only their own otherwise.</summary>
    private Guid? OwnerToList(Guid? requested)
    {
        if (currentUser.IsAdministrator)
        {
            return requested;
        }

        if (requested is { } other && other != currentUser.UserId)
        {
            throw new BookingListForbiddenException(other);
        }

        return currentUser.UserId;
    }
}
