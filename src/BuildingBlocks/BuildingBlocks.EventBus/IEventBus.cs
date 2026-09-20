namespace QubicaCinema.BuildingBlocks.EventBus;

/// <summary>
/// Sends an integration event to whoever is listening.
/// </summary>
/// <remarks>
/// Used by the outbox publisher and by nothing else. A use case never calls it: publishing from inside a
/// handler would escape the database transaction, and the outbox would be decorative.
/// </remarks>
public interface IEventBus
{
    /// <summary>
    /// Publishes one message and returns only once the broker has taken responsibility for it.
    /// </summary>
    /// <exception cref="EventPublishException">The broker refused or could not route the message.</exception>
    Task PublishAsync(OutgoingMessage message, CancellationToken cancellationToken);
}
