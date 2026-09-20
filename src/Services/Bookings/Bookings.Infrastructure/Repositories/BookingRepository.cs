using Microsoft.EntityFrameworkCore;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Infrastructure.Persistence;

namespace QubicaCinema.Bookings.Infrastructure.Repositories;

/// <inheritdoc cref="IBookingRepository" />
internal sealed class BookingRepository(BookingDbContext context) : IBookingRepository
{
    /// <inheritdoc />
    public Task<Booking?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Bookings
            // The items are the aggregate. A booking loaded without them would believe it holds nothing, and
            // cancelling it would cancel nothing and report success.
            .Include(booking => booking.Items)
            .FirstOrDefaultAsync(booking => booking.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Booking>> GetHoldingSeatsForAsync(
        Guid screeningId,
        CancellationToken cancellationToken) =>
        await context.Bookings
            // Every item, not only the ones at this screening: the booking decides for itself whether it is
            // left holding anything, and it can only do that if it can see all of its items.
            .Include(booking => booking.Items)
            .Where(booking => booking.Items.Any(item =>
                item.ScreeningId == screeningId && item.Status == BookingItemStatus.Active))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public void Add(Booking booking) => context.Bookings.Add(booking);

    /// <inheritdoc />
    public void Discard(Booking booking)
    {
        // Detaching the booking does not detach its items, and an item left behind would be inserted on
        // the next attempt with nothing to belong to.
        foreach (BookingItem item in booking.Items)
        {
            context.Entry(item).State = EntityState.Detached;
        }

        context.Entry(booking).State = EntityState.Detached;
    }
}
