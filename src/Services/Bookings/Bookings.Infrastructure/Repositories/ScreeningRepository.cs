using Microsoft.EntityFrameworkCore;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Infrastructure.Persistence;

namespace QubicaCinema.Bookings.Infrastructure.Repositories;

/// <inheritdoc cref="IScreeningRepository" />
internal sealed class ScreeningRepository(BookingDbContext context) : IScreeningRepository
{
    /// <inheritdoc />
    public Task<Screening?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Screenings.FirstOrDefaultAsync(screening => screening.Id == id, cancellationToken);

    /// <inheritdoc />
    public void Add(Screening screening) => context.Screenings.Add(screening);

    /// <inheritdoc />
    /// <remarks>Without tracking: the booking use cases read screenings to decide, and never change them.</remarks>
    public async Task<IReadOnlyCollection<Screening>> GetManyAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        await context.Screenings
            .AsNoTracking()
            .Where(screening => ids.Contains(screening.Id))
            .ToListAsync(cancellationToken);
}
