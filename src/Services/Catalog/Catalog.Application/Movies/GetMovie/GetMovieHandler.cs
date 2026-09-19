using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Abstractions.Queries;
using QubicaCinema.Catalog.Domain.Exceptions;

namespace QubicaCinema.Catalog.Application.Movies.GetMovie;

/// <inheritdoc cref="GetMovieQuery" />
/// <remarks>
/// Answers with the film or throws; it never returns null. That is what keeps the endpoint free of an
/// <c>if</c>: "there is no such film" is a 404, and the one exception handler already knows that.
/// </remarks>
public sealed class GetMovieHandler(IMovieQueries movies)
    : IQueryHandler<GetMovieQuery, Versioned<MovieView>>
{
    /// <inheritdoc />
    /// <exception cref="MovieNotFoundException">No film has that id.</exception>
    public async Task<Versioned<MovieView>> HandleAsync(GetMovieQuery query, CancellationToken cancellationToken) =>
        await movies.FindAsync(query.MovieId, cancellationToken)
        ?? throw new MovieNotFoundException(query.MovieId);
}
