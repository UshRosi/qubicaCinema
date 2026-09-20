namespace QubicaCinema.BuildingBlocks.Persistence.Outbox;

/// <summary>How the outbox publisher paces itself. Checked by <see cref="OutboxOptionsValidator"/>.</summary>
public sealed class OutboxOptions
{
    /// <summary>How many messages are claimed at once. A full batch is followed by another without waiting.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>How long the publisher sleeps when there was nothing to send. This is the latency of an event.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>How long a claimed batch stays reserved for the publisher that took it.</summary>
    public TimeSpan ClaimTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
