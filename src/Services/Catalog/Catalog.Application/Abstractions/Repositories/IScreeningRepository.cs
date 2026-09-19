using QubicaCinema.Catalog.Domain.Screenings;

namespace QubicaCinema.Catalog.Application.Abstractions.Repositories;

/// <summary>Loads and stores <see cref="Screening"/> aggregates.</summary>
public interface IScreeningRepository
{
    /// <summary>Loads a screening for editing, or null when there is none with that id.</summary>
    Task<Screening?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the slots an auditorium is already committed to within a window.
    /// </summary>
    /// <remarks>
    /// The window is what keeps the overlap check cheap: a new screening can only clash with something
    /// running around the same time, so there is no reason to read a whole season out of the database.
    /// Cancelled screenings are left out — they no longer occupy the room.
    /// </remarks>
    Task<IReadOnlyCollection<ScheduledSlot>> GetScheduleAsync(
        Guid auditoriumId,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken);

    /// <summary>Stages a new screening.</summary>
    void Add(Screening screening);

    /// <summary>Stages an edit, asserting the version the caller believes it is replacing.</summary>
    void Update(Screening screening, ReadOnlyMemory<byte> expectedVersion);
}
