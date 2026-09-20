using QubicaCinema.Bookings.Domain.Bookings;

namespace QubicaCinema.Bookings.Application.Abstractions.Repositories;

/// <summary>
/// Loads and stores <see cref="Booking"/> aggregates.
/// </summary>
/// <remarks>
/// No <c>SaveChangesAsync</c> here: committing is <see cref="BuildingBlocks.Domain.IUnitOfWork"/>'s job.
/// </remarks>
public interface IBookingRepository
{
    /// <summary>Loads a booking with every one of its items, or null when there is none with that id.</summary>
    Task<Booking?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads, with their items, every booking that still holds a seat at the given screening. Used when the
    /// screening is called off, to give those seats back.
    /// </summary>
    Task<IReadOnlyCollection<Booking>> GetHoldingSeatsForAsync(Guid screeningId, CancellationToken cancellationToken);

    /// <summary>Stages a new booking and its items.</summary>
    void Add(Booking booking);

    /// <summary>
    /// Forgets a booking that was staged but could not be committed, so that the next attempt does not
    /// save it alongside its replacement.
    /// </summary>
    void Discard(Booking booking);
}
