using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Abstractions.Queries;

namespace QubicaCinema.Catalog.Application.Movies.GetMovies;

/// <inheritdoc cref="GetMoviesQuery" />
public sealed class GetMoviesHandler(IMovieQueries movies)
    : IQueryHandler<GetMoviesQuery, PagedResult<MovieView>>
{
    /// <inheritdoc />
    public Task<PagedResult<MovieView>> HandleAsync(GetMoviesQuery query, CancellationToken cancellationToken) =>
        movies.GetPageAsync(query.Skip, query.Take, cancellationToken);
}
