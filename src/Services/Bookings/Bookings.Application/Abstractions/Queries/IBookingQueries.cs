using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Bookings.Application.Bookings;

namespace QubicaCinema.Bookings.Application.Abstractions.Queries;

/// <summary>
/// What the Booking service can be asked about bookings.
/// </summary>
/// <remarks>
/// Unlike Catalog's read side, implementations load the aggregate rather than projecting straight to the
/// view. The view shows the booking's total, and the total is defined once, by <c>Booking.Total</c>;
/// projecting it in SQL would be a second definition waiting to disagree with the first. A booking is a
/// handful of rows, so the cost is small.
/// </remarks>
public interface IBookingQueries
{
    /// <summary>One booking with its row version, or null when there is none with that id.</summary>
    Task<Versioned<BookingView>?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>A page of bookings, newest first, of one user or of everyone.</summary>
    /// <param name="userId">Whose bookings, or null for every user's.</param>
    /// <param name="skip">How many bookings to skip.</param>
    /// <param name="take">How many bookings to return.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<PagedResult<BookingView>> GetPageAsync(
        Guid? userId,
        int skip,
        int take,
        CancellationToken cancellationToken);
}
