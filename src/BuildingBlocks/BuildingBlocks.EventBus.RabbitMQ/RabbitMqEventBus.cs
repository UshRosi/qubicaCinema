using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <inheritdoc cref="IEventBus" />
/// <remarks>
/// Publishes on one channel it owns, one message at a time. A channel is not thread-safe, and a pool would
/// buy nothing: the only caller is the outbox publisher, which publishes in order and must not overlap
/// itself.
/// <para>
/// Every message is persistent, published with confirmations, and <c>mandatory</c>. The confirmation makes
/// <see cref="PublishAsync"/> return only once the broker has the message. <c>Mandatory</c> makes the broker
/// hand back a message that no queue would receive — a consumer's queue that has not been declared yet —
/// instead of silently dropping it, so the outbox keeps the row and tries again.
/// </para>
/// </remarks>
internal sealed class RabbitMqEventBus(
    RabbitMqConnectionProvider connections,
    IOptions<RabbitMqOptions> options) : IEventBus, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IChannel? _channel;

    /// <inheritdoc />
    public async Task PublishAsync(OutgoingMessage message, CancellationToken cancellationToken)
    {
        string exchange = options.Value.ExchangeName;

        await _gate.WaitAsync(cancellationToken);

        try
        {
            IChannel channel = await GetChannelAsync(exchange, cancellationToken);

            using Activity? activity = EventBusDiagnostics.StartPublish(message, exchange);

            var headers = new Dictionary<string, object?>();

            if (activity is not null)
            {
                EventBusDiagnostics.Inject(activity, headers);
            }

            var properties = new BasicProperties
            {
                MessageId = message.EventId.ToString(),
                Type = message.EventName,
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds()),
                Headers = headers,
            };

            try
            {
                await channel.BasicPublishAsync(
                    exchange,
                    routingKey: message.EventName,
                    mandatory: true,
                    properties,
                    Encoding.UTF8.GetBytes(message.Payload),
                    cancellationToken);
            }
            catch (PublishException exception)
            {
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);

                throw new EventPublishException(
                    exception.IsReturn
                        ? $"No queue is bound to receive '{message.EventName}' yet."
                        : $"The broker did not confirm '{message.EventName}'.",
                    exception);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        _gate.Dispose();
    }

    private async Task<IChannel> GetChannelAsync(string exchange, CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        // Closed and not recovered: start again rather than publish into a dead channel.
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        IConnection connection = await connections.GetConnectionAsync(cancellationToken);

        IChannel channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        await RabbitMqTopology.DeclareExchangesAsync(channel, exchange, cancellationToken);

        _channel = channel;

        return channel;
    }
}
