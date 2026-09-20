using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// Holds the one connection a process makes to the broker, created the first time it is needed.
/// </summary>
/// <remarks>
/// A provider rather than a registered <c>IConnection</c> because the version 7 client is async-only: a
/// connection cannot be awaited inside a DI factory delegate, and blocking on it there is how a service
/// deadlocks at startup. It is a singleton, guarded by a semaphore so that two callers arriving together
/// share one connection instead of making two.
/// <para>
/// Automatic recovery is on: after a network failure the client reconnects, and re-declares the topology
/// and the consumers, by itself. So once a connection exists this class hands it out even while it is
/// closed — replacing it would leave the original recovering in the background and duplicate every consumer.
/// Channels are deliberately not shared: they are not thread-safe, and each owner creates its own.
/// </para>
/// </remarks>
public sealed class RabbitMqConnectionProvider : IAsyncDisposable
{
    private const int MaxConnectAttempts = 5;

    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnectionProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    /// <summary>Prepares a provider for the broker at the given AMQP URI.</summary>
    /// <param name="amqpUri">For example <c>amqp://user:password@host:5672</c>.</param>
    /// <param name="logger">Where retries are reported.</param>
    public RabbitMqConnectionProvider(string amqpUri, ILogger<RabbitMqConnectionProvider> logger)
    {
        _logger = logger;
        _factory = new ConnectionFactory
        {
            Uri = new Uri(amqpUri),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = "qubica-cinema",
        };
    }

    /// <summary>Whether a connection exists and is open right now.</summary>
    public bool IsConnected => _connection is { IsOpen: true };

    /// <summary>
    /// Returns the connection, creating it if this is the first call.
    /// </summary>
    /// <remarks>Retries a bounded number of times when the broker is not reachable yet, as it is while it starts.</remarks>
    /// <exception cref="BrokerUnreachableException">The broker could not be reached within the attempts.</exception>
    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            _connection ??= await ConnectAsync(cancellationToken);

            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }

    private async Task<IConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await _factory.CreateConnectionAsync(cancellationToken);
            }
            catch (BrokerUnreachableException exception) when (attempt < MaxConnectAttempts)
            {
                var delay = TimeSpan.FromSeconds(attempt);

                _logger.LogWarning(
                    exception,
                    "RabbitMQ is not reachable (attempt {Attempt} of {MaxAttempts}); retrying in {Delay}.",
                    attempt,
                    MaxConnectAttempts,
                    delay);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
