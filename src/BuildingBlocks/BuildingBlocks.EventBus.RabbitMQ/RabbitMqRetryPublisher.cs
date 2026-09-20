using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// Parks a failed message in the retry queue that matches its retry number, to come back after that
/// queue's delay.
/// </summary>
/// <remarks>
/// Owns a channel of its own, with publisher confirmations: the message is acknowledged on the queue it came
/// from only after the broker has confirmed it is safe in the retry queue, so a failure in between can never
/// lose it — at worst it is processed twice, and the inbox absorbs that. Separate from the consuming channel
/// because a channel is not thread-safe and the two are used from different places.
/// </remarks>
internal sealed class RabbitMqRetryPublisher(IChannel channel, string queueName)
{
    /// <summary>The header carrying how many times this message has already been retried.</summary>
    internal const string RetryCountHeader = "x-retry-count";

    /// <summary>How many times the delivery has already been retried; zero for a first delivery.</summary>
    internal static int RetryCountOf(BasicDeliverEventArgs delivery) =>
        delivery.BasicProperties.Headers is { } headers
        && headers.TryGetValue(RetryCountHeader, out object? value)
        && value is int count
            ? count
            : 0;

    /// <summary>Republishes the message into its retry queue and waits for the broker to confirm it.</summary>
    internal Task ScheduleAsync(BasicDeliverEventArgs delivery, RetryDecision decision, CancellationToken cancellationToken)
    {
        // Every header except the broker's own, which start with "x-" and are stamped again on delivery. The
        // trace context is among what is kept, so the retry stays in the same trace.
        var headers = new Dictionary<string, object?>();

        foreach (KeyValuePair<string, object?> header in delivery.BasicProperties.Headers ?? new Dictionary<string, object?>())
        {
            if (!header.Key.StartsWith("x-", StringComparison.Ordinal))
            {
                headers[header.Key] = header.Value;
            }
        }

        headers[RetryCountHeader] = decision.RetryNumber;

        var properties = new BasicProperties
        {
            MessageId = delivery.BasicProperties.MessageId,
            Type = delivery.BasicProperties.Type,
            ContentType = delivery.BasicProperties.ContentType,
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = delivery.BasicProperties.Timestamp,
            Headers = headers,
        };

        // The default exchange routes by queue name, straight to the retry queue.
        return channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: RabbitMqTopology.RetryQueue(queueName, decision.RetryNumber),
            mandatory: true,
            properties,
            delivery.Body,
            cancellationToken).AsTask();
    }
}
