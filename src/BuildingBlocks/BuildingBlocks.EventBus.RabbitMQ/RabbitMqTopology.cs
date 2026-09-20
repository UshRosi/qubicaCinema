using RabbitMQ.Client;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// Declares the exchanges and queues the bus uses. Declaring is idempotent, so every process does it for
/// what it needs and the order in which the services start does not matter.
/// </summary>
internal static class RabbitMqTopology
{
    /// <summary>The exchange dead-lettered messages are sent to.</summary>
    internal static string DeadLetterExchange(string exchange) => $"{exchange}.dead-letter";

    /// <summary>The queue that keeps what a consumer could not process, for a human to inspect.</summary>
    internal static string DeadLetterQueue(string queue) => $"{queue}.dead-letter";

    /// <summary>
    /// The queue that holds a message for the delay of its <paramref name="retryNumber"/>th retry, counting from one.
    /// </summary>
    internal static string RetryQueue(string queue, int retryNumber) => $"{queue}.retry.{retryNumber}";

    /// <summary>Declares the two exchanges. A publisher needs only this.</summary>
    internal static async Task DeclareExchangesAsync(
        IChannel channel,
        string exchange,
        CancellationToken cancellationToken)
    {
        // Topic, so a consumer binds to exactly the event names it understands.
        await channel.ExchangeDeclareAsync(
            exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);

        // Direct, keyed by queue name, so each consumer's failures land in its own dead-letter queue.
        await channel.ExchangeDeclareAsync(
            DeadLetterExchange(exchange), ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);
    }

    /// <summary>Declares a consumer's queue, its dead-letter queue, and one binding per event it handles.</summary>
    internal static async Task DeclareQueueAsync(
        IChannel channel,
        RabbitMqOptions options,
        string queue,
        IEnumerable<string> eventNames,
        CancellationToken cancellationToken)
    {
        await DeclareExchangesAsync(channel, options.ExchangeName, cancellationToken);

        string deadLetterExchange = DeadLetterExchange(options.ExchangeName);
        string deadLetterQueue = DeadLetterQueue(queue);

        await channel.QueueDeclareAsync(
            deadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            deadLetterQueue, deadLetterExchange, routingKey: queue, cancellationToken: cancellationToken);

        // Retry queues, one per delay. A message put in one waits there, unread, until its time-to-live runs
        // out, and is then dead-lettered to the default exchange with the consumer's queue name as routing
        // key, which delivers it back to that one queue. Back to the queue and not to the events exchange:
        // going through the topic exchange again would hand the message to every other service's queue too.
        // The delay lives in the queue, not in the message, because only the message at the head of a queue
        // can expire; one queue per delay keeps every message in a queue equally old.
        for (int retry = 1; retry <= options.RetryDelays.Count; retry++)
        {
            await channel.QueueDeclareAsync(
                RetryQueue(queue, retry),
                durable: true,
                exclusive: false,
                autoDelete: false,
                new Dictionary<string, object?>
                {
                    ["x-queue-type"] = "quorum",
                    ["x-message-ttl"] = (long)options.RetryDelays[retry - 1].TotalMilliseconds,
                    ["x-dead-letter-exchange"] = string.Empty,
                    ["x-dead-letter-routing-key"] = queue,
                },
                cancellationToken: cancellationToken);
        }

        // Rejecting a message without requeue sends it to the dead-letter exchange and so to the dead-letter
        // queue. The delivery limit is only a safety net for a consumer that keeps crashing on a message
        // before it can decide anything: the broker counts unacknowledged redeliveries, and gives up after
        // as many deliveries as the message could legitimately have had.
        var arguments = new Dictionary<string, object?>
        {
            ["x-queue-type"] = "quorum",
            ["x-delivery-limit"] = options.RetryDelays.Count + 1,
            ["x-dead-letter-exchange"] = deadLetterExchange,
            ["x-dead-letter-routing-key"] = queue,
        };

        await channel.QueueDeclareAsync(
            queue, durable: true, exclusive: false, autoDelete: false, arguments, cancellationToken: cancellationToken);

        foreach (string eventName in eventNames)
        {
            await channel.QueueBindAsync(queue, options.ExchangeName, eventName, cancellationToken: cancellationToken);
        }
    }
}
