using Microsoft.EntityFrameworkCore;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Infrastructure.Persistence;

namespace QubicaCinema.Bookings.Infrastructure.Repositories;

/// <inheritdoc cref="ISeatRepository" />
internal sealed class SeatRepository(BookingDbContext context) : ISeatRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetKnownIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        (await context.Seats
            .AsNoTracking()
            .Where(seat => ids.Contains(seat.Id))
            .Select(seat => seat.Id)
            .ToListAsync(cancellationToken))
        .ToHashSet();

    /// <inheritdoc />
    public void AddRange(IEnumerable<Seat> seats) => context.Seats.AddRange(seats);
}
