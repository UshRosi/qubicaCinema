using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Abstractions.Queries;
using QubicaCinema.Catalog.Application.Screenings;
using QubicaCinema.Catalog.Domain.Screenings;
using QubicaCinema.Catalog.Infrastructure.Persistence;
using QubicaCinema.Catalog.Infrastructure.Persistence.Configurations;

namespace QubicaCinema.Catalog.Infrastructure.Queries;

/// <inheritdoc cref="IScreeningQueries" />
/// <remarks>
/// The joins are written out because a <see cref="Screening"/> holds only the ids of its movie and its
/// auditorium — the model refuses to carry navigation properties between aggregates, so the read side pays
/// for that decision here, once, in one <c>join</c> per query rather than in a lookup per row.
/// </remarks>
internal sealed class ScreeningQueries(CatalogDbContext context) : IScreeningQueries
{
    /// <inheritdoc />
    public async Task<PagedResult<ScreeningView>> GetPageAsync(
        ScreeningFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        IQueryable<Screening> screenings = Filtered(filter);

        // Counted before the join: both joins are inner joins on required foreign keys, so they cannot
        // change how many screenings match, and counting the narrower query is the cheaper statement.
        int totalCount = await screenings.CountAsync(cancellationToken);

        List<ScreeningView> page = await RowsOf(Ordered(screenings, filter.Sort))
            .Skip(skip)
            .Take(take)
            .Select(row => row.View)
            .ToListAsync(cancellationToken);

        return new PagedResult<ScreeningView>(page, totalCount);
    }

    /// <inheritdoc />
    public async Task<Versioned<ScreeningView>?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        ScreeningRow? found = await RowsOf(context.Screenings.AsNoTracking().Where(screening => screening.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

        return found is null ? null : new Versioned<ScreeningView>(found.View, found.Version);
    }

    /// <summary>
    /// Joins screenings to their movie and auditorium and projects the view.
    /// </summary>
    /// <remarks>
    /// Written once and used by both the list and the single read, so they cannot drift into returning
    /// different shapes of the same resource.
    /// </remarks>
    private IQueryable<ScreeningRow> RowsOf(IQueryable<Screening> screenings) =>
        from screening in screenings
        join movie in context.Movies on screening.MovieId equals movie.Id
        join auditorium in context.Auditoriums on screening.AuditoriumId equals auditorium.Id
        select new ScreeningRow(
            new ScreeningView(
                screening.Id,
                movie.Id,
                movie.Title,
                auditorium.Id,
                auditorium.Name,
                screening.Slot.StartsAt,
                screening.Slot.EndsAt,
                screening.Price.Amount,
                screening.Price.Currency,
                screening.Status),
            EF.Property<byte[]>(screening, RowVersion.Name));

    private IQueryable<Screening> Filtered(ScreeningFilter filter)
    {
        IQueryable<Screening> screenings = context.Screenings.AsNoTracking();

        if (!filter.IncludeCancelled)
        {
            screenings = screenings.Where(screening => screening.Status == ScreeningStatus.Scheduled);
        }

        if (filter.MovieId is { } movieId)
        {
            screenings = screenings.Where(screening => screening.MovieId == movieId);
        }

        if (filter.AuditoriumId is { } auditoriumId)
        {
            screenings = screenings.Where(screening => screening.AuditoriumId == auditoriumId);
        }

        if (filter.From is { } from)
        {
            screenings = screenings.Where(screening => screening.Slot.StartsAt >= from);
        }

        if (filter.To is { } until)
        {
            screenings = screenings.Where(screening => screening.Slot.StartsAt < until);
        }

        return screenings;
    }

    /// <summary>
    /// Orders the screenings before they are joined and projected.
    /// </summary>
    /// <remarks>
    /// Over the entity, not over the view. Ordering by a member of an object that the query itself
    /// constructs is not something the translator can turn into an <c>ORDER BY</c>, and the only way to
    /// satisfy it would be to fetch the rows and sort them in memory — which would silently make every
    /// page of the programme a full table read.
    /// </remarks>
    private static IQueryable<Screening> Ordered(IQueryable<Screening> screenings, ScreeningSort sort) =>
        (sort switch
        {
            ScreeningSort.StartingLatest => screenings.OrderByDescending(screening => screening.Slot.StartsAt),
            ScreeningSort.CheapestFirst => screenings.OrderBy(screening => screening.Price.Amount),
            ScreeningSort.DearestFirst => screenings.OrderByDescending(screening => screening.Price.Amount),
            _ => screenings.OrderBy(screening => screening.Slot.StartsAt),
        })
            // Every ordering ends with the id, so that two screenings at the same time or the same price
            // cannot swap places between page one and page two and hide a row.
            .ThenBy(screening => screening.Id);

    /// <summary>
    /// A screening view and the version of the row it came from. Private to this class: it exists only so
    /// that one projection can serve both queries.
    /// </summary>
    /// <remarks>
    /// Reading the version on list queries too costs one narrow column, and buys a single projection that
    /// both the list and the single read go through — worth more than the column.
    /// </remarks>
    private sealed record ScreeningRow(ScreeningView View, byte[] Version);
}
