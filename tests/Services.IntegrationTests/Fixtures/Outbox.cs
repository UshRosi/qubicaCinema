using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.EventBus;
using QubicaCinema.BuildingBlocks.Persistence.Outbox;
using QubicaCinema.Catalog.Infrastructure.Persistence;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>Pumps Catalog's outbox by hand, since <c>Messaging:RunOutboxPublisher</c> is off in this tier.</summary>
internal static class Outbox
{
    /// <summary>
    /// Publishes every waiting row, batch by batch, until none are left.
    /// </summary>
    /// <remarks>
    /// <see cref="IOutboxProcessor.ProcessBatchAsync"/> stops at the first failure within a batch by design —
    /// a screening's cancellation must never overtake its own announcement — so this loop surfaces that
    /// failure immediately rather than retrying it into a timeout with no message.
    /// </remarks>
    internal static async Task DrainAsync(CatalogApp catalog, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = catalog.Services.CreateAsyncScope();
        IOutboxProcessor processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        OutboxBatchResult result;

        do
        {
            result = await processor.ProcessBatchAsync(cancellationToken);

            if (result.Failed)
            {
                throw new InvalidOperationException("Publishing an outbox message failed; see the logged warning for the reason.");
            }
        }
        while (result.MoreWaiting);
    }

    /// <summary>
    /// Publishes an already-published row a second time, over the real broker — a redelivery, exactly the
    /// shape <c>InboxTests</c> needs to prove that a second delivery of the same <c>EventId</c> is a no-op.
    /// </summary>
    /// <returns>The <c>EventId</c> that was republished, the inbox's own deduplication key.</returns>
    internal static async Task<Guid> RepublishAsync(CatalogApp catalog, string eventName, Guid containing, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = catalog.Services.CreateAsyncScope();
        CatalogDbContext context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        IEventBus eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        OutboxMessage row = await context.Set<OutboxMessage>()
            .Where(message => message.EventName == eventName)
            .Where(message => EF.Functions.Like(message.Payload, $"%{containing}%"))
            .SingleAsync(cancellationToken);

        await eventBus.PublishAsync(row.ToOutgoing(), cancellationToken);

        return row.Id;
    }
}
