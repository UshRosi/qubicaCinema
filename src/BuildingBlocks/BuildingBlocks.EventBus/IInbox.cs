namespace QubicaCinema.BuildingBlocks.EventBus;

/// <summary>
/// The record of which events this service has already applied, so that a redelivered message changes
/// nothing.
/// </summary>
/// <remarks>
/// Brokers deliver at least once: an acknowledgement can be lost after the work is done, and the message
/// then comes back. The inbox turns that at-least-once delivery into effectively-once processing.
/// </remarks>
public interface IInbox
{
    /// <summary>Whether this event has already been applied.</summary>
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Stages the mark for an event just handled. Committed with the handler's own changes.</summary>
    void MarkProcessed(Guid eventId, string eventName);
}
