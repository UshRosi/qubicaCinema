using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Auditoriums;

namespace QubicaCinema.Catalog.Application.Abstractions.Queries;

/// <summary>
/// What the catalogue can be asked about its screening rooms.
/// </summary>
/// <remarks>
/// Two shapes on purpose: a list without seats, because a page of rooms with full seat maps is thousands of
/// rows to answer "what rooms are there?", and a single room with every seat in it.
/// </remarks>
public interface IAuditoriumQueries
{
    /// <summary>A page of auditoriums, ordered by name, without their seat maps.</summary>
    Task<PagedResult<AuditoriumView>> GetPageAsync(int skip, int take, CancellationToken cancellationToken);

    /// <summary>One auditorium and every seat in it, or null when there is none with that id.</summary>
    Task<AuditoriumDetailView?> FindAsync(Guid id, CancellationToken cancellationToken);
}
