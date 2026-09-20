using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.EventBus;

namespace QubicaCinema.BuildingBlocks.Persistence.Inbox;

/// <inheritdoc cref="IInbox" />
/// <remarks>
/// Generic over the context for the reason the idempotency store is: the mark must be saved by the same
/// <c>DbContext</c>, in the same transaction, as the handler's own changes. A shared database would not give
/// that guarantee.
/// </remarks>
/// <typeparam name="TContext">The service's <c>DbContext</c>, which owns the inbox table.</typeparam>
public sealed class EfInbox<TContext>(TContext context, TimeProvider clock) : IInbox
    where TContext : DbContext
{
    /// <inheritdoc />
    public Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken) =>
        context.Set<InboxMessage>()
            .AsNoTracking()
            .AnyAsync(message => message.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public void MarkProcessed(Guid eventId, string eventName) =>
        context.Set<InboxMessage>().Add(new InboxMessage(eventId, eventName, clock.GetUtcNow()));
}
