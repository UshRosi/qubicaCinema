using Microsoft.EntityFrameworkCore;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Infrastructure.Persistence;

namespace QubicaCinema.Bookings.Infrastructure.Repositories;

/// <inheritdoc cref="ISeatMapRepository" />
internal sealed class SeatMapRepository(BookingDbContext context) : ISeatMapRepository
{
    /// <inheritdoc />
    public async Task<SeatMap?> FindAsync(Guid screeningId, CancellationToken cancellationToken)
    {
        Screening? screening = await context.Screenings
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == screeningId, cancellationToken);

        if (screening is null)
        {
            return null;
        }

        List<Seat> seats = await context.Seats
            .AsNoTracking()
            .Where(seat => seat.AuditoriumId == screening.AuditoriumId)
            .ToListAsync(cancellationToken);

        // Served by the filtered unique index: its key starts with the screening and it holds only active
        // items, so this is a seek over exactly the rows that answer the question.
        List<Guid> bookedSeatIds = await context.BookingItems
            .AsNoTracking()
            .Where(item => item.ScreeningId == screeningId && item.Status == BookingItemStatus.Active)
            .Select(item => item.SeatId)
            .ToListAsync(cancellationToken);

        return new SeatMap(screening, seats, bookedSeatIds);
    }
}
