using System.Diagnostics;
using QubicaCinema.BuildingBlocks.Contracts;
using QubicaCinema.BuildingBlocks.EventBus;

namespace QubicaCinema.BuildingBlocks.Persistence.Outbox;

/// <summary>
/// An integration event waiting in the database to be published.
/// </summary>
/// <remarks>
/// Written in the same transaction as the change that caused it, which is the only way to know that a state
/// change and its announcement either both happened or neither did. A separate publisher then delivers it,
/// and can be down for a week without losing anything.
/// </remarks>
public sealed class OutboxMessage
{
    /// <summary>The longest error text kept. A stack trace in a row is noise, and an unbounded one is a risk.</summary>
    public const int MaxErrorLength = 2000;

    private OutboxMessage(Guid id, string eventName, string payload, DateTimeOffset occurredAt)
    {
        Id = id;
        EventName = eventName;
        Payload = payload;
        OccurredAt = occurredAt;
    }

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private OutboxMessage()
    {
        EventName = string.Empty;
        Payload = string.Empty;
    }

    /// <summary>The event's own id, so the row and the consumer's deduplication key are the same value.</summary>
    public Guid Id { get; private set; }

    /// <summary>The stable wire name. Never a CLR type name: renaming a class must not orphan stored rows.</summary>
    public string EventName { get; private set; }

    /// <summary>The event as JSON, exactly as it will be published.</summary>
    public string Payload { get; private set; }

    /// <summary>When it happened.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>When it was published, or null while it is still waiting.</summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>The W3C <c>traceparent</c> of the operation that raised the event.</summary>
    /// <remarks>
    /// Captured when the row is written and restored when it is published. Without it the dashboard would
    /// show two unrelated traces, the API call and the delivery, and the causal chain would be invisible.
    /// </remarks>
    public string? TraceParent { get; private set; }

    /// <summary>The W3C <c>tracestate</c> that travels with <see cref="TraceParent"/>.</summary>
    public string? TraceState { get; private set; }

    /// <summary>How many times publishing has failed. A message that keeps failing is visible, not silent.</summary>
    public int Attempts { get; private set; }

    /// <summary>Why the last attempt failed.</summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Until when a publisher owns this row. A row claimed by a publisher that then crashed becomes
    /// available again once this time has passed.
    /// </summary>
    public DateTimeOffset? ClaimedUntil { get; private set; }

    /// <summary>Stages an event for publishing, remembering the trace it was raised in.</summary>
    /// <param name="integrationEvent">The event to store.</param>
    /// <param name="activity">The operation in progress, usually <see cref="Activity.Current"/>.</param>
    public static OutboxMessage From(IntegrationEvent integrationEvent, Activity? activity) =>
        new(
            integrationEvent.EventId,
            integrationEvent.EventName,
            IntegrationEventSerializer.Serialize(integrationEvent),
            integrationEvent.OccurredAt)
        {
            TraceParent = activity?.Id,
            TraceState = activity?.TraceStateString,
        };

    /// <summary>The message in the shape the bus publishes.</summary>
    public OutgoingMessage ToOutgoing() => new(Id, EventName, Payload, OccurredAt, TraceParent, TraceState);

    /// <summary>Records that the broker has taken the message.</summary>
    public void MarkPublished(DateTimeOffset now)
    {
        ProcessedAt = now;
        ClaimedUntil = null;
        LastError = null;
    }

    /// <summary>Records a failed attempt and gives the row back so that it is tried again.</summary>
    public void RecordFailure(string error)
    {
        Attempts++;
        LastError = error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];
        ClaimedUntil = null;
    }

    /// <summary>Gives the row back without counting an attempt: it was claimed but never tried.</summary>
    public void Release() => ClaimedUntil = null;
}
