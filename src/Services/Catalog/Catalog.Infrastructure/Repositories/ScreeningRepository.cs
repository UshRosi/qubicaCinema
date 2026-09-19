using Microsoft.EntityFrameworkCore;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Domain.Screenings;
using QubicaCinema.Catalog.Infrastructure.Persistence;

namespace QubicaCinema.Catalog.Infrastructure.Repositories;

/// <inheritdoc cref="IScreeningRepository" />
internal sealed class ScreeningRepository(CatalogDbContext context) : IScreeningRepository
{
    /// <inheritdoc />
    public Task<Screening?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Screenings.FirstOrDefaultAsync(screening => screening.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ScheduledSlot>> GetScheduleAsync(
        Guid auditoriumId,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken) =>
        await context.Screenings
            .AsNoTracking()
            .Where(screening =>
                screening.AuditoriumId == auditoriumId
                && screening.Status == ScreeningStatus.Scheduled
                // Half-open on both sides, matching TimeSlot.Overlaps: anything touching the window only at
                // an endpoint does not occupy it.
                && screening.Slot.StartsAt < windowEnd
                && screening.Slot.EndsAt > windowStart)
            .Select(screening => new ScheduledSlot(screening.Id, screening.Slot))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public void Add(Screening screening) => context.Screenings.Add(screening);

    /// <inheritdoc />
    public void Update(Screening screening, ReadOnlyMemory<byte> expectedVersion) =>
        ConcurrencyGuard.RequireVersion(context.Entry(screening), expectedVersion, screening.Id);
}
