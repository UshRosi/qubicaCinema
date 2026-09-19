using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Abstractions.Queries;
using QubicaCinema.Catalog.Application.Movies;
using QubicaCinema.Catalog.Infrastructure.Persistence;
using QubicaCinema.Catalog.Infrastructure.Persistence.Configurations;

namespace QubicaCinema.Catalog.Infrastructure.Queries;

/// <inheritdoc cref="IMovieQueries" />
/// <remarks>
/// Every query is <c>AsNoTracking</c> and projects straight into the view: no aggregate is materialised, no
/// change-tracking entry is created, and the SQL asks for exactly the columns in the response.
/// </remarks>
internal sealed class MovieQueries(CatalogDbContext context) : IMovieQueries
{
    /// <inheritdoc />
    public async Task<PagedResult<MovieView>> GetPageAsync(int skip, int take, CancellationToken cancellationToken)
    {
        // Counting first and then reading the page is two round trips on purpose: the alternative is
        // reading everything in order to count it, which is the thing paging exists to avoid.
        int totalCount = await context.Movies.CountAsync(cancellationToken);

        List<MovieView> page = await context.Movies
            .AsNoTracking()
            .OrderBy(movie => movie.Title)
            .ThenBy(movie => movie.Id)
            .Skip(skip)
            .Take(take)
            .Select(movie => new MovieView(
                movie.Id,
                movie.Title,
                movie.Description,
                (int)movie.Duration.TotalMinutes,
                movie.Genre,
                movie.AgeRating))
            .ToListAsync(cancellationToken);

        return new PagedResult<MovieView>(page, totalCount);
    }

    /// <inheritdoc />
    public async Task<Versioned<MovieView>?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await context.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == id)
            .Select(movie => new
            {
                Movie = new MovieView(
                    movie.Id,
                    movie.Title,
                    movie.Description,
                    (int)movie.Duration.TotalMinutes,
                    movie.Genre,
                    movie.AgeRating),
                // The row version is a shadow property, so it is read by name rather than off the model.
                Version = EF.Property<byte[]>(movie, RowVersion.Name),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return found is null ? null : new Versioned<MovieView>(found.Movie, found.Version);
    }
}
