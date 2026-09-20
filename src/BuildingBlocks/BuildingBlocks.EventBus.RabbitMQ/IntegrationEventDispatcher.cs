using Microsoft.Extensions.DependencyInjection;
using QubicaCinema.BuildingBlocks.Contracts;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// Turns a payload into a typed event and hands it to the handler registered for it.
/// </summary>
/// <remarks>
/// The non-generic base lets the consumer keep every dispatcher in one dictionary keyed by wire name, while
/// the generic subclass keeps the event and handler types. The consumer therefore never uses reflection to
/// find a handler: the types were captured, at compile time, in <c>Subscribe&lt;TEvent, THandler&gt;</c>.
/// </remarks>
internal abstract class IntegrationEventDispatcher
{
    /// <exception cref="System.Text.Json.JsonException">The payload is not a valid event of this type.</exception>
    internal abstract IntegrationEvent Deserialize(string payload);

    internal abstract Task DispatchAsync(
        IServiceProvider services,
        IntegrationEvent integrationEvent,
        CancellationToken cancellationToken);
}

/// <inheritdoc />
internal sealed class IntegrationEventDispatcher<TEvent, THandler> : IntegrationEventDispatcher
    where TEvent : IntegrationEvent
    where THandler : class, IIntegrationHandler<TEvent>
{
    internal override IntegrationEvent Deserialize(string payload) =>
        IntegrationEventSerializer.Deserialize<TEvent>(payload);

    internal override Task DispatchAsync(
        IServiceProvider services,
        IntegrationEvent integrationEvent,
        CancellationToken cancellationToken) =>
        services.GetRequiredService<THandler>().HandleAsync((TEvent)integrationEvent, cancellationToken);
}
