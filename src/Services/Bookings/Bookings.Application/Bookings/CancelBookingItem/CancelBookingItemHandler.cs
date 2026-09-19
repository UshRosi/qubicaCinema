using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Bookings.Application.Abstractions.Queries;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.Bookings.CancelBookingItem;

/// <inheritdoc cref="CancelBookingItemCommand" />
/// <remarks>Giving back the last seat still held cancels the whole booking; the aggregate decides that.</remarks>
public sealed class CancelBookingItemHandler(
    IBookingRepository bookings,
    IScreeningRepository screenings,
    IUnitOfWork unitOfWork,
    IBookingQueries queries,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<CancelBookingItemCommand, Versioned<BookingView>>
{
    /// <inheritdoc />
    /// <exception cref="BookingNotFoundException">No such booking, or it belongs to someone else.</exception>
    /// <exception cref="BookingItemNotFoundException">The booking has no such item.</exception>
    /// <exception cref="BookingNotCancellableException">The seat is for a screening that has started.</exception>
    public async Task<Versioned<BookingView>> HandleAsync(
        CancelBookingItemCommand command,
        CancellationToken cancellationToken)
    {
        Booking? booking = await bookings.FindAsync(command.BookingId, cancellationToken);

        if (booking is null || !currentUser.MayAccess(booking.UserId))
        {
            throw new BookingNotFoundException(command.BookingId);
        }

        IReadOnlyCollection<Screening> schedule = await screenings.GetManyAsync(booking.ScreeningIds, cancellationToken);

        booking.CancelItem(command.ItemId, schedule, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await queries.FindAsync(booking.Id, cancellationToken)
               ?? throw new BookingNotFoundException(booking.Id);
    }
}
