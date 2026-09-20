using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QubicaCinema.BuildingBlocks.Contracts;
using QubicaCinema.BuildingBlocks.Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// Consumes the service's queue and applies each event exactly once.
/// </summary>
/// <remarks>
/// Every message goes through the same steps, in a scope of its own, and the order is the point:
/// <list type="number">
///   <item>An event name nobody here handles is logged and acknowledged. The queue is bound only to names
///   this service subscribed to, so it can only be a misrouted message, and retrying it would never help.</item>
///   <item>A payload that cannot be read is rejected without requeue, which sends it to the dead-letter
///   queue: no number of retries will make it parse.</item>
///   <item>An event already in the inbox is acknowledged and dropped.</item>
///   <item>The handler runs, the inbox row is staged, and one <c>SaveChanges</c> writes both, so the work and
///   the record that it was done are one transaction.</item>
///   <item>Only then is the message acknowledged. A crash between the save and the ack redelivers the
///   message, and step 3 turns the redelivery into a no-op.</item>
/// </list>
/// A handler that fails is not requeued at once: an immediate retry would burn every attempt in a few
/// milliseconds, well inside a database restart. <see cref="EventFailureClassifier"/> separates the failures
/// that time can cure from those it cannot. A transient one is parked in a retry queue whose delay grows with
/// each attempt (<see cref="RabbitMqRetryPolicy"/>) and comes back to this queue when it expires; a permanent
/// one, or one that has used up its retries, goes to the dead-letter queue, where a person can look at it.
/// The token passed in by the host is not given to the handler: a delivery in flight should finish or fail
/// cleanly, not half-commit because the process is shutting down.
/// </remarks>
internal sealed class RabbitMqConsumerService(
    RabbitMqConnectionProvider connections,
    SubscriptionRegistry registry,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    RabbitMqRetryPolicy retryPolicy,
    ILogger<RabbitMqConsumerService> logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(10);

    // Held while a message is being handled, so that shutdown can wait for the delivery in flight.
    private readonly SemaphoreSlim _handling = new(1, 1);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The broker may not be up yet, or may go away. The consumer must outlive both: a background service
        // that throws stops the whole host, and Booking's reads do not depend on the broker at all.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilStoppedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Consuming '{Queue}' failed; trying again in {Delay}.",
                    registry.QueueName,
                    ReconnectDelay);

                await DelayAsync(stoppingToken);
            }
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _handling.Dispose();
        base.Dispose();
    }

    private async Task ConsumeUntilStoppedAsync(CancellationToken stoppingToken)
    {
        RabbitMqOptions current = options.Value;

        IConnection connection = await connections.GetConnectionAsync(stoppingToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // A second channel, with confirmations, for parking failed messages: see RabbitMqRetryPublisher.
        await using IChannel retryChannel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            stoppingToken);
        var retries = new RabbitMqRetryPublisher(retryChannel, registry.QueueName);

        await RabbitMqTopology.DeclareQueueAsync(channel, current, registry.QueueName, registry.EventNames, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, (ushort)current.PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, retries, delivery);

        string consumerTag = await channel.BasicConsumeAsync(
            registry.QueueName, autoAck: false, consumer, stoppingToken);

        logger.LogInformation(
            "Consuming '{Queue}' for {EventNames}.",
            registry.QueueName,
            string.Join(", ", registry.EventNames));

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            await StopConsumingAsync(channel, consumerTag);
        }
    }

    /// <summary>Stops taking new messages, then gives the one in flight a moment to finish and be acknowledged.</summary>
    private async Task StopConsumingAsync(IChannel channel, string consumerTag)
    {
        try
        {
            await channel.BasicCancelAsync(consumerTag);

            if (!await _handling.WaitAsync(DrainTimeout))
            {
                logger.LogWarning("A delivery was still being handled after {Timeout}; it will be redelivered.", DrainTimeout);
            }
            else
            {
                _handling.Release();
            }
        }
        catch (Exception exception)
        {
            // Best effort while shutting down: the broker requeues anything unacknowledged anyway.
            logger.LogWarning(exception, "Could not stop the consumer of '{Queue}' cleanly.", registry.QueueName);
        }
    }

    private async Task HandleDeliveryAsync(IChannel channel, RabbitMqRetryPublisher retries, BasicDeliverEventArgs delivery)
    {
        await _handling.WaitAsync();

        try
        {
            await ProcessAsync(channel, retries, delivery);
        }
        finally
        {
            _handling.Release();
        }
    }

    private async Task ProcessAsync(IChannel channel, RabbitMqRetryPublisher retries, BasicDeliverEventArgs delivery)
    {
        // The type the publisher stamped, not the routing key: a message that has been through a retry queue
        // arrives with the queue's name as its routing key, but still carries its own type.
        string eventName = delivery.BasicProperties.Type ?? delivery.RoutingKey;

        using Activity? activity = EventBusDiagnostics.StartReceive(delivery, eventName);

        try
        {
            if (!registry.TryGetDispatcher(eventName, out IntegrationEventDispatcher? dispatcher))
            {
                logger.LogWarning("No handler is subscribed to '{EventName}'; acknowledging and discarding it.", eventName);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);

                return;
            }

            IntegrationEvent integrationEvent;

            try
            {
                integrationEvent = dispatcher.Deserialize(Encoding.UTF8.GetString(delivery.Body.Span));
            }
            catch (JsonException exception)
            {
                logger.LogError(exception, "'{EventName}' could not be read; sending it to the dead-letter queue.", eventName);
                activity?.SetStatus(ActivityStatusCode.Error, "Unreadable payload");
                await TryRejectAsync(channel, delivery, requeue: false);

                return;
            }

            EventBusDiagnostics.TagMessageId(activity, integrationEvent.EventId);

            await ApplyAsync(dispatcher, integrationEvent);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);

            await HandleFailureAsync(channel, retries, delivery, eventName, exception);
        }
    }

    /// <summary>Runs the handler and records the event as processed, in one transaction.</summary>
    private async Task ApplyAsync(IntegrationEventDispatcher dispatcher, IntegrationEvent integrationEvent)
    {
        // CreateAsyncScope, not CreateScope: the DbContext inside is IAsyncDisposable.
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        var inbox = services.GetRequiredService<IInbox>();

        if (await inbox.HasProcessedAsync(integrationEvent.EventId, CancellationToken.None))
        {
            logger.LogInformation("{EventName} {EventId} was already applied; skipping it.", integrationEvent.EventName, integrationEvent.EventId);

            return;
        }

        await dispatcher.DispatchAsync(services, integrationEvent, CancellationToken.None);

        inbox.MarkProcessed(integrationEvent.EventId, integrationEvent.EventName);
        await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

    /// <summary>Retries the message later, or dead-letters it, as <see cref="RabbitMqRetryPolicy"/> decides.</summary>
    private async Task HandleFailureAsync(
        IChannel channel,
        RabbitMqRetryPublisher retries,
        BasicDeliverEventArgs delivery,
        string eventName,
        Exception exception)
    {
        RetryDecision decision = retryPolicy.Decide(RabbitMqRetryPublisher.RetryCountOf(delivery), exception);

        if (decision.GiveUp)
        {
            logger.LogError(
                exception,
                "Handling '{EventName}' failed and will not be retried ({Reason}); sending it to the dead-letter queue.",
                eventName,
                decision.Reason);

            await TryRejectAsync(channel, delivery, requeue: false);

            return;
        }

        logger.LogWarning(
            exception,
            "Handling '{EventName}' failed; retry {Retry} of {MaxRetries} in {Delay}.",
            eventName,
            decision.RetryNumber,
            retryPolicy.MaxRetries,
            decision.Delay);

        try
        {
            // Confirmed in the retry queue first, acknowledged here second: a failure in between duplicates
            // the message rather than losing it, and the inbox makes a duplicate harmless.
            await retries.ScheduleAsync(delivery, decision, CancellationToken.None);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
        }
        catch (Exception scheduling)
        {
            // The retry queue is out of reach, so the delay cannot be honoured. Give the message back at once;
            // the queue's delivery limit still ends the loop in the dead-letter queue if this keeps failing.
            logger.LogWarning(scheduling, "Could not schedule the retry of '{EventName}'; requeueing it immediately.", eventName);

            await TryRejectAsync(channel, delivery, requeue: true);
        }
    }

    private async Task TryRejectAsync(IChannel channel, BasicDeliverEventArgs delivery, bool requeue)
    {
        try
        {
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue);
        }
        catch (Exception exception)
        {
            // The channel is gone, and the broker returns an unacknowledged message to the queue by itself.
            logger.LogWarning(exception, "Could not reject a message; the broker will redeliver it.");
        }
    }

    private static async Task DelayAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ReconnectDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down while waiting to retry: the loop's condition ends it.
        }
    }
}
