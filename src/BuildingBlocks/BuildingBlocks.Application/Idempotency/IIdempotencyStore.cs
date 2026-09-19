namespace QubicaCinema.BuildingBlocks.Application.Idempotency;

/// <summary>
/// Runs an operation at most once per idempotency key, and remembers what it answered.
/// </summary>
/// <remarks>
/// Implemented by each service's Infrastructure against its own database, because the one property that
/// makes the pattern safe is that the record of the key is written in the <em>same transaction</em> as the
/// operation's own changes. Written separately, a crash between the two would leave either a booking
/// nobody can replay or a key that claims a booking that was never made.
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>
    /// Runs <paramref name="operation"/> unless the key has been seen, and records its response in the
    /// same transaction as its changes.
    /// </summary>
    /// <param name="request">The key, its owner, and the fingerprint of the request.</param>
    /// <param name="operation">
    /// The work to do once. It may be invoked more than once if the database connection fails transiently,
    /// always from a clean slate, and only one invocation is ever committed.
    /// </param>
    /// <param name="cancellationToken">Cancels the whole unit.</param>
    Task<IdempotencyOutcome> ExecuteOnceAsync(
        IdempotentRequest request,
        Func<CancellationToken, Task<RecordedResponse>> operation,
        CancellationToken cancellationToken);
}
