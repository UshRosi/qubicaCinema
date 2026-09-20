using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QubicaCinema.BuildingBlocks.EventBus;

namespace QubicaCinema.BuildingBlocks.Persistence.Outbox;

/// <inheritdoc cref="IOutboxProcessor" />
/// <remarks>
/// Messages are published in order and publishing stops at the first failure. A screening's cancellation
/// must never overtake the announcement of the screening itself, and skipping the message that failed would
/// allow exactly that. One stuck message therefore holds back the ones behind it, which is the right
/// trade-off for events that describe a sequence of changes to one thing.
/// </remarks>
/// <typeparam name="TContext">The service's <c>DbContext</c>, which owns the outbox table.</typeparam>
public sealed class OutboxProcessor<TContext>(
    TContext context,
    IEventBus eventBus,
    IOptionsMonitor<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxProcessor<TContext>> logger) : IOutboxProcessor
    where TContext : DbContext
{
    // UPDLOCK and READPAST make two publishers take different rows instead of blocking each other, and the
    // update writes the claim in the same statement that selects the rows, so nothing can slip in between.
    // The rows are ordered by when they happened: a version 7 id would sort by time in memory, but SQL
    // Server does not sort a uniqueidentifier that way.
    private const string ClaimSql = $"""
        WITH [batch] AS (
            SELECT TOP (@batchSize) *
            FROM [{OutboxMessageConfiguration.TableName}] WITH (UPDLOCK, READPAST, ROWLOCK)
            WHERE [ProcessedAt] IS NULL AND ([ClaimedUntil] IS NULL OR [ClaimedUntil] < @now)
            ORDER BY [OccurredAt], [Id])
        UPDATE [batch] SET [ClaimedUntil] = @claimedUntil
        OUTPUT inserted.[Id] AS [Value]
        """;

    /// <inheritdoc />
    public async Task<OutboxBatchResult> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        OutboxOptions current = options.CurrentValue;
        List<OutboxMessage> batch = await ClaimAsync(current, cancellationToken);

        int published = 0;

        for (int index = 0; index < batch.Count; index++)
        {
            OutboxMessage message = batch[index];

            try
            {
                await eventBus.PublishAsync(message.ToOutgoing(), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    exception,
                    "Publishing {EventName} {EventId} failed (attempt {Attempts}); it will be retried.",
                    message.EventName,
                    message.Id,
                    message.Attempts + 1);

                message.RecordFailure(exception.Message);

                // Claimed but never tried: give them back now rather than after the claim timeout.
                foreach (OutboxMessage untouched in batch.Skip(index + 1))
                {
                    untouched.Release();
                }

                await context.SaveChangesAsync(CancellationToken.None);

                return new OutboxBatchResult(current.BatchSize, published, Failed: true);
            }

            // Saved one message at a time: a crash half way through a batch must not republish what was
            // already delivered.
            message.MarkPublished(clock.GetUtcNow());
            await context.SaveChangesAsync(cancellationToken);
            published++;
        }

        return new OutboxBatchResult(current.BatchSize, published, Failed: false);
    }

    private async Task<List<OutboxMessage>> ClaimAsync(OutboxOptions current, CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.GetUtcNow();

        List<Guid> claimed = await context.Database
            .SqlQueryRaw<Guid>(
                ClaimSql,
                new SqlParameter("@batchSize", current.BatchSize),
                new SqlParameter("@now", now),
                new SqlParameter("@claimedUntil", now + current.ClaimTimeout))
            .ToListAsync(cancellationToken);

        if (claimed.Count == 0)
        {
            return [];
        }

        // The OUTPUT clause makes no promise about order, so the rows are read again in the order they
        // must be published in.
        return await context.Set<OutboxMessage>()
            .Where(message => claimed.Contains(message.Id))
            .OrderBy(message => message.OccurredAt)
            .ThenBy(message => message.Id)
            .ToListAsync(cancellationToken);
    }
}
