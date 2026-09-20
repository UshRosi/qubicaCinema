using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client.Events;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// The tracing vocabulary of the bus, and the only place that spells it: the activity source, the span names,
/// the tag names and values of OpenTelemetry's messaging conventions, and how a trace context travels through
/// message headers. The publisher and the consumer ask for a span here and never write a tag themselves, so
/// the two sides cannot drift apart.
/// </summary>
internal static class EventBusDiagnostics
{
    /// <summary>
    /// The name of the activity source of every span the bus starts. It falls under the
    /// <c>QubicaCinema.*</c> wildcard that the service defaults subscribe to, so no service has to list it.
    /// Declared here rather than in the service defaults because this project cannot reference them.
    /// </summary>
    internal const string SourceName = "QubicaCinema.EventBus";

    internal static readonly ActivitySource Source = new(SourceName);

    // Tag names and values of the OpenTelemetry messaging semantic conventions.
    private const string SystemTag = "messaging.system";
    private const string OperationTag = "messaging.operation.name";
    private const string DestinationTag = "messaging.destination.name";
    private const string RoutingKeyTag = "messaging.rabbitmq.destination.routing_key";
    private const string MessageIdTag = "messaging.message.id";

    private const string SystemName = "rabbitmq";
    private const string PublishOperation = "publish";
    private const string ReceiveOperation = "receive";

    private static readonly Action<IDictionary<string, object?>, string, string> Set =
        static (headers, key, value) => headers[key] = value;

    private static readonly Func<IDictionary<string, object?>, string, IEnumerable<string>?> Get =
        static (headers, key) => headers.TryGetValue(key, out object? value) && value is byte[] bytes
            ? [Encoding.UTF8.GetString(bytes)]
            : null;

    /// <summary>
    /// Starts the producer span as a child of the operation that raised the event, not of whatever the
    /// publisher loop happens to be doing, so the trace runs from the API call through the outbox to here.
    /// </summary>
    internal static Activity? StartPublish(OutgoingMessage message, string exchange)
    {
        ActivityContext parent = default;

        if (message.TraceParent is not null)
        {
            _ = ActivityContext.TryParse(message.TraceParent, message.TraceState, out parent);
        }

        Activity? activity = Source.StartActivity($"{exchange} {PublishOperation}", ActivityKind.Producer, parent);

        Tag(activity, PublishOperation, exchange, message.EventName);
        activity?.SetTag(MessageIdTag, message.EventId.ToString());

        return activity;
    }

    /// <summary>Starts the consumer span as a child of the publisher's, so one trace crosses the broker.</summary>
    internal static Activity? StartReceive(BasicDeliverEventArgs delivery, string eventName)
    {
        PropagationContext parent = Extract(delivery.BasicProperties.Headers);
        Baggage.Current = parent.Baggage;

        Activity? activity = Source.StartActivity(
            $"{eventName} {ReceiveOperation}", ActivityKind.Consumer, parent.ActivityContext);

        Tag(activity, ReceiveOperation, delivery.Exchange, eventName);

        return activity;
    }

    /// <summary>Adds the message's id to a consumer span once the payload has been read.</summary>
    internal static void TagMessageId(Activity? activity, Guid eventId) =>
        activity?.SetTag(MessageIdTag, eventId.ToString());

    /// <summary>Writes the span's trace context, and the current baggage, into message headers.</summary>
    internal static void Inject(Activity activity, IDictionary<string, object?> headers) =>
        Propagators.DefaultTextMapPropagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current), headers, Set);

    private static PropagationContext Extract(IDictionary<string, object?>? headers) =>
        headers is null
            ? default
            : Propagators.DefaultTextMapPropagator.Extract(default, headers, Get);

    private static void Tag(Activity? activity, string operation, string destination, string routingKey)
    {
        activity?.SetTag(SystemTag, SystemName);
        activity?.SetTag(OperationTag, operation);
        activity?.SetTag(DestinationTag, destination);
        activity?.SetTag(RoutingKeyTag, routingKey);
    }
}
