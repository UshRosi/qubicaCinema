namespace QubicaCinema.BuildingBlocks.Persistence.Outbox;

/// <summary>What one pass over the outbox achieved.</summary>
/// <param name="BatchSize">How many messages a full batch holds.</param>
/// <param name="Published">How many the broker accepted.</param>
/// <param name="Failed">Whether publishing stopped at a message the broker refused.</param>
public sealed record OutboxBatchResult(int BatchSize, int Published, bool Failed)
{
    /// <summary>
    /// Whether the publisher should go round again without sleeping: the batch was full and nothing went
    /// wrong, so there is probably a backlog. After a failure it must wait instead, or it would hammer a
    /// broker that has just said no.
    /// </summary>
    public bool MoreWaiting => !Failed && Published == BatchSize;
}
