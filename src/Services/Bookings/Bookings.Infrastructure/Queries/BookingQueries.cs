using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Bookings.Application.Abstractions.Queries;
using QubicaCinema.Bookings.Application.Bookings;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Infrastructure.Persistence;
using QubicaCinema.Bookings.Infrastructure.Persistence.Configurations;

namespace QubicaCinema.Bookings.Infrastructure.Queries;

/// <inheritdoc cref="IBookingQueries" />
internal sealed class BookingQueries(BookingDbContext context) : IBookingQueries
{
    /// <inheritdoc />
    /// <remarks>
    /// Tracked, unlike every other read here, for the row version: it is a shadow property, and a tracked
    /// entry is the one place EF exposes it on a loaded aggregate. It is one booking. It also means that
    /// after a cancellation in the same request this returns the instance just saved, with the version the
    /// database has just given it — exactly the ETag the response should carry.
    /// </remarks>
    public async Task<Versioned<BookingView>?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        Booking? booking = await context.Bookings
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (booking is null)
        {
            return null;
        }

        BookingView[] view = await DescribeAsync([booking], cancellationToken);
        byte[] version = context.Entry(booking).Property<byte[]>(RowVersion.Name).CurrentValue;

        return new Versioned<BookingView>(view[0], version);
    }

    /// <inheritdoc />
    public async Task<PagedResult<BookingView>> GetPageAsync(
        Guid? userId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        IQueryable<Booking> bookings = context.Bookings.AsNoTracking();

        if (userId is { } owner)
        {
            bookings = bookings.Where(booking => booking.UserId == owner);
        }

        int totalCount = await bookings.CountAsync(cancellationToken);

        List<Booking> page = await bookings
            .OrderByDescending(booking => booking.CreatedAt)
            .ThenBy(booking => booking.Id)
            .Skip(skip)
            .Take(take)
            .Include(booking => booking.Items)
            // One query for the bookings and one for their items, rather than a join that repeats every
            // booking's columns once per seat.
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResult<BookingView>(await DescribeAsync(page, cancellationToken), totalCount);
    }

    /// <summary>
    /// Turns bookings into views, reading the screenings and seats they refer to in one query each rather
    /// than once per item.
    /// </summary>
    private async Task<BookingView[]> DescribeAsync(IReadOnlyCollection<Booking> bookings, CancellationToken cancellationToken)
    {
        Guid[] screeningIds = [.. bookings.SelectMany(booking => booking.Items).Select(item => item.ScreeningId).Distinct()];
        Guid[] seatIds = [.. bookings.SelectMany(booking => booking.Items).Select(item => item.SeatId).Distinct()];

        var screenings = await context.Screenings
            .AsNoTracking()
            .Where(screening => screeningIds.Contains(screening.Id))
            .ToDictionaryAsync(screening => screening.Id, cancellationToken);

        var seats = await context.Seats
            .AsNoTracking()
            .Where(seat => seatIds.Contains(seat.Id))
            .ToDictionaryAsync(seat => seat.Id, cancellationToken);

        return [.. bookings.Select(booking => BookingView.From(booking, screenings, seats))];
    }
}
