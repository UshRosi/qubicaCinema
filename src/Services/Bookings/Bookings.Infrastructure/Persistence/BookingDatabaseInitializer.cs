using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using QubicaCinema.BuildingBlocks.Persistence;
using QubicaCinema.Bookings.Infrastructure.Persistence.Seeding;

namespace QubicaCinema.Bookings.Infrastructure.Persistence;

/// <summary>Brings <c>bookingdb</c> up to date and, until chapter 3, fills its read model.</summary>
internal sealed class BookingDatabaseInitializer(
    BookingDbContext context,
    TimeProvider clock,
    ILogger<BookingDatabaseInitializer> logger) : IDatabaseInitializer
{
    /// <inheritdoc />
    public string DatabaseName => BookingsInfrastructureExtensions.DatabaseName;

    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken cancellationToken) =>
        context.Database.MigrateAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>No bookings are seeded: a reviewer makes the first one, which is the point of the demo.</remarks>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Screenings.AnyAsync(cancellationToken))
        {
            logger.LogInformation("{Database} already holds screenings; leaving it alone.", DatabaseName);
            return;
        }

        // Handed to the execution strategy as one unit, so a transient failure replays the whole seed.
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);

            BookingReadModelSeed.Write(context, clock);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        logger.LogInformation("Seeded {Database} with a stand-in programme of screenings and seats.", DatabaseName);
    }
}
