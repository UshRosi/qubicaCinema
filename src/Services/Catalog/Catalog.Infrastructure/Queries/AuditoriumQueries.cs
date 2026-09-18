using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Abstractions.Queries;
using QubicaCinema.Catalog.Application.Auditoriums;
using QubicaCinema.Catalog.Infrastructure.Persistence;

namespace QubicaCinema.Catalog.Infrastructure.Queries;

/// <inheritdoc cref="IAuditoriumQueries" />
internal sealed class AuditoriumQueries(CatalogDbContext context) : IAuditoriumQueries
{
    /// <inheritdoc />
    public async Task<PagedResult<AuditoriumView>> GetPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        int totalCount = await context.Auditoriums.CountAsync(cancellationToken);

        List<AuditoriumView> page = await context.Auditoriums
            .AsNoTracking()
            .OrderBy(auditorium => auditorium.Name)
            .Skip(skip)
            .Take(take)
            .Select(auditorium => new AuditoriumView(
                auditorium.Id,
                auditorium.Name,
                auditorium.RowCount,
                auditorium.SeatsPerRow,
                auditorium.RowCount * auditorium.SeatsPerRow))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditoriumView>(page, totalCount);
    }

    /// <inheritdoc />
    public Task<AuditoriumDetailView?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Auditoriums
            .AsNoTracking()
            .Where(auditorium => auditorium.Id == id)
            .Select(auditorium => new AuditoriumDetailView(
                auditorium.Id,
                auditorium.Name,
                auditorium.RowCount,
                auditorium.SeatsPerRow,
                auditorium.RowCount * auditorium.SeatsPerRow,
                auditorium.Seats
                    .OrderBy(seat => seat.Position.Row)
                    .ThenBy(seat => seat.Position.Number)
                    .Select(seat => new SeatView(seat.Id, seat.Position.Row, seat.Position.Number))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}
