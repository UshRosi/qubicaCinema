namespace QubicaCinema.BuildingBlocks.EventBus;

/// <summary>
/// An integration event ready to leave the service: already serialized, with the trace context it was
/// raised in.
/// </summary>
/// <remarks>
/// The payload is serialized once, when the outbox row is written, and never goes back through a CLR type.
/// A message that has sat in the outbox for a day is published exactly as it was written, whatever has been
/// renamed or refactored since.
/// </remarks>
/// <param name="EventId">The consumer's deduplication key.</param>
/// <param name="EventName">The stable wire name, used as the routing key.</param>
/// <param name="Payload">The event, as JSON.</param>
/// <param name="OccurredAt">When it happened.</param>
/// <param name="TraceParent">The W3C <c>traceparent</c> of the operation that raised it, if any.</param>
/// <param name="TraceState">The W3C <c>tracestate</c> that went with it, if any.</param>
public sealed record OutgoingMessage(
    Guid EventId,
    string EventName,
    string Payload,
    DateTimeOffset OccurredAt,
    string? TraceParent,
    string? TraceState);
