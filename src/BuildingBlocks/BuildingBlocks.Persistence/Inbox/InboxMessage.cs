namespace QubicaCinema.BuildingBlocks.Persistence.Inbox;

/// <summary>
/// The fact that this service has applied one event.
/// </summary>
/// <remarks>
/// The event's id is the key, so a second attempt to record the same event fails on the primary key even if
/// two deliveries race each other past the existence check: the loser's transaction is rolled back, the
/// message is redelivered, and this time the check finds the row.
/// </remarks>
public sealed class InboxMessage
{
    internal InboxMessage(Guid eventId, string eventName, DateTimeOffset processedAt)
    {
        EventId = eventId;
        EventName = eventName;
        ProcessedAt = processedAt;
    }

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private InboxMessage() => EventName = string.Empty;

    /// <summary>The event's id, as its publisher assigned it.</summary>
    public Guid EventId { get; private set; }

    /// <summary>The wire name of the event, kept so that a row explains itself.</summary>
    public string EventName { get; private set; }

    /// <summary>When it was applied.</summary>
    public DateTimeOffset ProcessedAt { get; private set; }
}
