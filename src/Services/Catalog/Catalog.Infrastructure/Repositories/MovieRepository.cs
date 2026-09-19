using Microsoft.EntityFrameworkCore;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Domain.Movies;
using QubicaCinema.Catalog.Infrastructure.Persistence;

namespace QubicaCinema.Catalog.Infrastructure.Repositories;

/// <inheritdoc cref="IMovieRepository" />
internal sealed class MovieRepository(CatalogDbContext context) : IMovieRepository
{
    /// <inheritdoc />
    public Task<Movie?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Movies.FirstOrDefaultAsync(movie => movie.Id == id, cancellationToken);

    /// <inheritdoc />
    public void Add(Movie movie) => context.Movies.Add(movie);

    /// <inheritdoc />
    public void Update(Movie movie, ReadOnlyMemory<byte> expectedVersion) =>
        ConcurrencyGuard.RequireVersion(context.Entry(movie), expectedVersion, movie.Id);
}
