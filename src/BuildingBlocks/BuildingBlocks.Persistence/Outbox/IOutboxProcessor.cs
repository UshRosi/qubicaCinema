namespace QubicaCinema.BuildingBlocks.Persistence.Outbox;

/// <summary>
/// Publishes one batch of waiting outbox messages.
/// </summary>
/// <remarks>
/// A port of its own, not a private detail of the background service, so that an integration test can pump
/// the outbox deterministically instead of waiting for a timer.
/// </remarks>
public interface IOutboxProcessor
{
    /// <summary>Claims a batch, publishes it in order, and reports what happened.</summary>
    Task<OutboxBatchResult> ProcessBatchAsync(CancellationToken cancellationToken);
}
