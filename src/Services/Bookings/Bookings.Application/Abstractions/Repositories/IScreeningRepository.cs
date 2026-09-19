using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.Abstractions.Repositories;

/// <summary>Reads the local copy of the screenings that bookings refer to.</summary>
public interface IScreeningRepository
{
    /// <summary>The screenings with these ids that are known here. Unknown ids are simply absent.</summary>
    Task<IReadOnlyCollection<Screening>> GetManyAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
}
