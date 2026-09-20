using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.Abstractions.Repositories;

/// <summary>
/// Stores the seats that Catalog announced. A seat belongs to an auditorium, and the same seat is offered
/// at every screening in that room, so it is stored once however many screenings mention it.
/// </summary>
public interface ISeatRepository
{
    /// <summary>Which of these seat ids are already stored.</summary>
    Task<IReadOnlySet<Guid>> GetKnownIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Stages seats that were not stored yet.</summary>
    void AddRange(IEnumerable<Seat> seats);
}
