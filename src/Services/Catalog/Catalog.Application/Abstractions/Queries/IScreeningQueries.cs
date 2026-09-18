using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Screenings;

namespace QubicaCinema.Catalog.Application.Abstractions.Queries;

/// <summary>
/// What the catalogue can be asked about its programme.
/// </summary>
public interface IScreeningQueries
{
    /// <summary>A page of screenings matching the filter, in the order it asks for.</summary>
    Task<PagedResult<ScreeningView>> GetPageAsync(
        ScreeningFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>One screening with its row version, or null when there is none with that id.</summary>
    Task<Versioned<ScreeningView>?> FindAsync(Guid id, CancellationToken cancellationToken);
}
