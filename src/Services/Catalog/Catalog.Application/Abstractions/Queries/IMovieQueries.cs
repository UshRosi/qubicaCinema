using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Movies;

namespace QubicaCinema.Catalog.Application.Abstractions.Queries;

/// <summary>
/// What the catalogue can be asked about its films.
/// </summary>
/// <remarks>
/// The read side is kept apart from the repositories on purpose. A read never needs an aggregate: loading a
/// <c>Movie</c> only to copy its fields into a response costs change tracking and a wider query than the
/// answer requires. Implementations project straight from the query to the view with no tracking. The
/// write side still goes through the aggregates, because that is where the invariants live.
/// </remarks>
public interface IMovieQueries
{
    /// <summary>A page of films, ordered by title.</summary>
    Task<PagedResult<MovieView>> GetPageAsync(int skip, int take, CancellationToken cancellationToken);

    /// <summary>One film with its row version, or null when there is none with that id.</summary>
    Task<Versioned<MovieView>?> FindAsync(Guid id, CancellationToken cancellationToken);
}
