using System.Diagnostics.CodeAnalysis;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// What one service subscribes to: its queue, and a dispatcher for each event name it understands.
/// </summary>
internal sealed class SubscriptionRegistry(string queueName, IReadOnlyDictionary<string, IntegrationEventDispatcher> dispatchers)
{
    /// <summary>The queue this service consumes from.</summary>
    internal string QueueName { get; } = queueName;

    /// <summary>The event names the queue is bound to.</summary>
    internal IEnumerable<string> EventNames => dispatchers.Keys;

    internal bool TryGetDispatcher(string eventName, [NotNullWhen(true)] out IntegrationEventDispatcher? dispatcher) =>
        dispatchers.TryGetValue(eventName, out dispatcher);
}
