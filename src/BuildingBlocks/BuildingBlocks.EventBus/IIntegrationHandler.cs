using QubicaCinema.BuildingBlocks.Contracts;

namespace QubicaCinema.BuildingBlocks.EventBus;

/// <summary>
/// Reacts to one kind of integration event.
/// </summary>
/// <remarks>
/// A handler stages its changes and never commits. The consumer commits once, after the handler returns,
/// so the handler's work and the inbox row that marks the event as processed are written in one
/// transaction: either both exist or neither does, which is what makes redelivery safe.
/// </remarks>
/// <typeparam name="TEvent">The event it reacts to.</typeparam>
public interface IIntegrationHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    /// <summary>Applies the event to this service's own model.</summary>
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
