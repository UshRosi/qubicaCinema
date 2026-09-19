using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using QubicaCinema.BuildingBlocks.Persistence;
using QubicaCinema.Catalog.Infrastructure.Persistence.Seeding;

namespace QubicaCinema.Catalog.Infrastructure.Persistence;

/// <summary>
/// Brings <c>catalogdb</c> up to date and gives a reviewer something to look at.
/// </summary>
internal sealed class CatalogDatabaseInitializer(
    CatalogDbContext context,
    TimeProvider clock,
    ILogger<CatalogDatabaseInitializer> logger) : IDatabaseInitializer
{
    /// <inheritdoc />
    public string DatabaseName => "catalogdb";

    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken cancellationToken) =>
        context.Database.MigrateAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// The seed goes through the aggregates — <c>Auditorium.Create</c>, <c>Movie.Create</c>,
    /// <c>Screening.Schedule</c> — rather than through <c>ExecuteSqlRaw</c> or an <c>HasData</c> block.
    /// It is slower and it is the point: the demo data is then data the model would actually accept, the
    /// overlap rule is exercised on every start, and from chapter 3 the same path will produce real outbox
    /// rows instead of a database full of screenings nobody ever announced.
    /// </remarks>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Movies.AnyAsync(cancellationToken))
        {
            logger.LogInformation("{Database} already holds a catalogue; leaving it alone.", DatabaseName);
            return;
        }

        // EF refuses to start a transaction under a retrying execution strategy unless the whole unit is
        // handed to the strategy: a retry has to replay the transaction, not resume it. SQL Server
        // containers also accept connections slightly before they can answer queries, so the first attempt
        // here is the one most likely to need the retry.
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);

            CatalogSeed.Write(context, clock);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        logger.LogInformation("Seeded {Database} with the demo catalogue.", DatabaseName);
    }
}
