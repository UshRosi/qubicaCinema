using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Bookings.Application.Abstractions.Queries;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.Bookings.CancelBooking;

/// <inheritdoc cref="CancelBookingCommand" />
/// <remarks>
/// No <c>If-Match</c>, as with cancelling a screening in Catalog: asking twice gives back the seats once,
/// and wanting a booking cancelled does not depend on what the caller last read. The booking's row version
/// still guards the write, so a cancellation racing another change to the same booking cannot overwrite it.
/// </remarks>
public sealed class CancelBookingHandler(
    IBookingRepository bookings,
    IScreeningRepository screenings,
    IUnitOfWork unitOfWork,
    IBookingQueries queries,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<CancelBookingCommand, Versioned<BookingView>>
{
    /// <inheritdoc />
    /// <exception cref="BookingNotFoundException">No such booking, or it belongs to someone else.</exception>
    /// <exception cref="BookingNotCancellableException">A seat is for a screening that has started.</exception>
    public async Task<Versioned<BookingView>> HandleAsync(
        CancelBookingCommand command,
        CancellationToken cancellationToken)
    {
        Booking? booking = await bookings.FindAsync(command.BookingId, cancellationToken);

        if (booking is null || !currentUser.MayAccess(booking.UserId))
        {
            throw new BookingNotFoundException(command.BookingId);
        }

        IReadOnlyCollection<Screening> schedule = await screenings.GetManyAsync(booking.ScreeningIds, cancellationToken);

        booking.Cancel(schedule, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await queries.FindAsync(booking.Id, cancellationToken)
               ?? throw new BookingNotFoundException(booking.Id);
    }
}
