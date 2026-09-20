using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.BuildingBlocks.Persistence;
using QubicaCinema.BuildingBlocks.Persistence.Idempotency;
using QubicaCinema.BuildingBlocks.Persistence.Inbox;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Infrastructure.Persistence.Configurations;

namespace QubicaCinema.Bookings.Infrastructure.Persistence;

/// <summary>
/// The Booking database, and the <see cref="IUnitOfWork"/> of every Booking use case.
/// </summary>
public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options), IUnitOfWork
{
    /// <summary>The bookings, with their items.</summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>Every seat ever booked, read on its own to know which seats are taken.</summary>
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();

    /// <summary>The local copy of Catalog's screenings.</summary>
    public DbSet<Screening> Screenings => Set<Screening>();

    /// <summary>The local copy of Catalog's seats.</summary>
    public DbSet<Seat> Seats => Set<Seat>();

    /// <summary>
    /// Commits the use case, versioning each changed booking as a whole and translating the store's
    /// failures into the domain's own exceptions.
    /// </summary>
    /// <remarks>
    /// Translation happens here and nowhere else, so that no Application class ever sees a
    /// <c>DbUpdateException</c> or a SQL error number.
    /// </remarks>
    /// <exception cref="SeatAlreadyBookedException">Another booking took one of the seats first.</exception>
    /// <exception cref="ConcurrentModificationException">Someone else changed the same booking first.</exception>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        VersionBookingsWithChangedItems();

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            object id = exception.Entries.Count > 0
                ? exception.Entries[0].Property(nameof(Entity<Guid>.Id)).CurrentValue ?? "unknown"
                : "unknown";

            throw new ConcurrentModificationException(nameof(Booking), id, exception);
        }
        catch (DbUpdateException exception)
            when (SqlServerErrors.IsUniqueViolation(exception, BookingItemConfiguration.DoubleBookingIndexName))
        {
            throw await DescribeDoubleBookingAsync(exception, cancellationToken);
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);

        // The idempotency keys of POST /bookings. Their table is a building block: the same shape serves
        // any service, and it must live in this database so that a key and the booking it protects are
        // written in one transaction.
        modelBuilder.AddIdempotencyRecords();

        // Which of Catalog's events have already been applied, so that a redelivered message changes
        // nothing. In this database for the same reason: the mark and the change are one transaction.
        modelBuilder.AddInboxMessages();

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Makes a booking's row version move when only one of its items changed.
    /// </summary>
    /// <remarks>
    /// The row version sits on the <c>Bookings</c> row, and cancelling one seat changes only a
    /// <c>BookingItems</c> row — so without this, two requests cancelling the last two seats of a booking
    /// would each see one seat left, each cancel theirs, and leave a confirmed booking holding nothing.
    /// Marking the booking itself modified makes EF update its row with the version it was read at, and the
    /// second request is refused. The aggregate is the unit of consistency, so it is also the unit of
    /// versioning.
    /// </remarks>
    private void VersionBookingsWithChangedItems()
    {
        HashSet<Guid> changedBookingIds = [.. ChangeTracker.Entries<BookingItem>()
            .Where(item => item.State == EntityState.Modified)
            .Select(item => item.Property<Guid>(BookingItemConfiguration.BookingIdProperty).CurrentValue)];

        foreach (EntityEntry<Booking> booking in ChangeTracker.Entries<Booking>())
        {
            if (booking.State == EntityState.Unchanged && changedBookingIds.Contains(booking.Entity.Id))
            {
                booking.State = EntityState.Modified;
            }
        }
    }

    /// <summary>
    /// Works out which of the seats being booked were the ones taken, so the client is told exactly which
    /// to choose again rather than "something in your basket".
    /// </summary>
    /// <remarks>
    /// The index violation names one duplicate key, and a booking may hold several seats at several
    /// screenings; asking the database which of them are now held is exact and costs one indexed query on
    /// a path that is already a failure.
    /// </remarks>
    private async Task<SeatAlreadyBookedException> DescribeDoubleBookingAsync(
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        IGrouping<Guid, Guid>[] attempted = [.. ChangeTracker.Entries<BookingItem>()
            .Where(item => item.State == EntityState.Added)
            .GroupBy(item => item.Entity.ScreeningId, item => item.Entity.SeatId)];

        foreach (IGrouping<Guid, Guid> screening in attempted)
        {
            Guid[] seatIds = [.. screening];

            List<Guid> taken = await BookingItems
                .AsNoTracking()
                .Where(item => item.ScreeningId == screening.Key
                               && item.Status == BookingItemStatus.Active
                               && seatIds.Contains(item.SeatId))
                .Select(item => item.SeatId)
                .ToListAsync(cancellationToken);

            if (taken.Count > 0)
            {
                return new SeatAlreadyBookedException(screening.Key, taken, exception);
            }
        }

        // The booking that won has been cancelled again in the meantime; the seats are free now, but this
        // attempt still lost. Report every seat it asked for, so the client simply tries again.
        IGrouping<Guid, Guid> first = attempted[0];

        return new SeatAlreadyBookedException(first.Key, [.. first], exception);
    }
}
