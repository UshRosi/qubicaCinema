using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.Abstractions.Repositories;

/// <summary>Reads the local copy of the screenings that bookings refer to.</summary>
public interface IScreeningRepository
{
    /// <summary>Loads one screening for change, or null when Catalog has not announced it yet.</summary>
    Task<Screening?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Stages a screening that Catalog announced.</summary>
    void Add(Screening screening);

    /// <summary>The screenings with these ids that are known here. Unknown ids are simply absent.</summary>
    Task<IReadOnlyCollection<Screening>> GetManyAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
}
