namespace QubicaCinema.BuildingBlocks.EventBus;

/// <summary>
/// The broker did not take a message: it was refused, or no queue was bound to receive it.
/// </summary>
/// <remarks>
/// Not a <c>DomainException</c>: no client request can cause it, and no client can fix it. The outbox
/// treats it as "try again later", which is the whole reason the message was stored first.
/// </remarks>
public sealed class EventPublishException : Exception
{
    /// <inheritdoc />
    public EventPublishException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
