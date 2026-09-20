using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.Persistence;

namespace QubicaCinema.Bookings.Infrastructure.Persistence;

/// <summary>Brings <c>bookingdb</c> up to date.</summary>
internal sealed class BookingDatabaseInitializer(BookingDbContext context) : IDatabaseInitializer
{
    /// <inheritdoc />
    public string DatabaseName => BookingsInfrastructureExtensions.DatabaseName;

    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken cancellationToken) =>
        context.Database.MigrateAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// Nothing to seed. The screenings and seats are Catalog's, and arrive as events; a copy invented here
    /// would carry ids that mean nothing to Catalog. No bookings are seeded either: a reviewer makes the
    /// first one, which is the point of the demo.
    /// </remarks>
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
