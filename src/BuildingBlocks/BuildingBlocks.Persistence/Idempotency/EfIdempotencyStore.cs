using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QubicaCinema.BuildingBlocks.Application.Idempotency;
namespace QubicaCinema.BuildingBlocks.Persistence.Idempotency;

/// <summary>
/// Keeps idempotency keys in a service's own database, in the same transaction as the changes they protect.
/// </summary>
/// <remarks>
/// The sequence, and why each step is where it is:
/// <list type="number">
/// <item><description>A key already recorded is answered straight away — a replay, or a reuse for a
/// different request — without running anything.</description></item>
/// <item><description>Otherwise a transaction opens and the key is claimed first. Inserting the record
/// takes the primary key's lock, so a duplicate request arriving now waits on it and then fails the
/// insert: that is the "in flight" answer, and it means two copies of one request can never both
/// book.</description></item>
/// <item><description>The operation runs inside the transaction. Its own <c>SaveChanges</c> is a
/// savepoint in it, which is what lets a lost seat race be rolled back and retried without losing the
/// claim.</description></item>
/// <item><description>The response is written onto the record and everything commits together: there is
/// no state in which a booking exists without its key, or a key without its booking.</description></item>
/// </list>
/// The whole unit runs under the execution strategy, because a retrying strategy refuses a transaction
/// it does not control: after a transient failure it replays the unit from the top, and the change
/// tracker is cleared first so the replay starts from nothing.
/// </remarks>
/// <typeparam name="TContext">
/// The service's own <see cref="DbContext"/>. Generic rather than one store per service: nothing in here
/// mentions bookings, and the property that matters — one transaction for the record and the work — only
/// holds if both go through the same context. A service adds the table with
/// <see cref="IdempotencyModelExtensions.AddIdempotencyRecords"/> and registers this against its context.
/// </typeparam>
public sealed class EfIdempotencyStore<TContext>(TContext context, TimeProvider clock) : IIdempotencyStore
    where TContext : DbContext
{
    /// <inheritdoc />
    public Task<IdempotencyOutcome> ExecuteOnceAsync(
        IdempotentRequest request,
        Func<CancellationToken, Task<RecordedResponse>> operation,
        CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(token => RunOnceAsync(request, operation, token), cancellationToken);
    }

    private async Task<IdempotencyOutcome> RunOnceAsync(
        IdempotentRequest request,
        Func<CancellationToken, Task<RecordedResponse>> operation,
        CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();

        IdempotencyRecord? existing = await context.Set<IdempotencyRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                record => record.UserId == request.UserId && record.Key == request.Key,
                cancellationToken);

        if (existing is not null)
        {
            return existing.AnswerRepeat(request.RequestHash);
        }

        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var record = IdempotencyRecord.Claim(request, clock.GetUtcNow());
        context.Set<IdempotencyRecord>().Add(record);

        if (!await TryClaimAsync(cancellationToken))
        {
            return new IdempotencyOutcome.InFlight();
        }

        RecordedResponse response = await operation(cancellationToken);

        record.Complete(response);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IdempotencyOutcome.Executed(response);
    }

    /// <summary>Inserts the claim; false when another request holds the same key.</summary>
    private async Task<bool> TryClaimAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateException exception)
            when (SqlServerErrors.IsUniqueViolation(exception, IdempotencyRecordConfiguration.PrimaryKeyName))
        {
            return false;
        }
    }
}
