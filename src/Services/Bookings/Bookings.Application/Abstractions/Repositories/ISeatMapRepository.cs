using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.Abstractions.Repositories;

/// <summary>Builds the seat map of a screening, as it stands at the moment of the read.</summary>
/// <remarks>
/// Three facts from three tables — the screening, the seats of its auditorium, the seats already held —
/// assembled into the one domain object that decides what is free. Nothing in the map is ever written back,
/// so implementations read without change tracking.
/// </remarks>
public interface ISeatMapRepository
{
    /// <summary>The seat map of the screening, or null when there is no screening with that id.</summary>
    Task<SeatMap?> FindAsync(Guid screeningId, CancellationToken cancellationToken);
}
