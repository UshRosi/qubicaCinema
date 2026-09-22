using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Testcontainers.RabbitMq;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>
/// One RabbitMQ container for the whole assembly, the same image the AppHost runs.
/// </summary>
/// <remarks>
/// Used both as the broker the three services connect to, and — through its management API — as the way
/// this fixture confirms that Booking's consumer has finished declaring its queue and bindings before any
/// test publishes into it. See <see cref="WaitForBookingBindingsAsync"/> for why that check cannot be
/// "does the queue exist".
/// </remarks>
internal sealed class RabbitMqFixture : IAsyncDisposable
{
    private const int ManagementPort = 15672;

    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4.3-management")
        .WithPortBinding(ManagementPort, assignRandomHostPort: true)
        .Build();

    private HttpClient? _management;

    /// <summary>Starts the container. Call once, before building any host that connects to it.</summary>
    internal Task StartAsync(CancellationToken cancellationToken) => _container.StartAsync(cancellationToken);

    /// <summary>The broker's AMQP connection string, in the same shape <c>ConnectionStrings__rabbitmq</c> carries.</summary>
    internal string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Waits until the <c>booking</c> queue is bound to every one of the given routing keys on the
    /// <c>qubica.events</c> exchange.
    /// </summary>
    /// <remarks>
    /// <c>RabbitMqTopology.DeclareQueueAsync</c> declares the <c>booking</c> queue and only then binds it,
    /// one call per event name — so the queue answering <c>200</c> on the management API is not enough:
    /// there is a real window where the queue exists but no routing key is bound to it yet, and a publish
    /// with <c>mandatory: true</c> in that window is returned to the publisher as unroutable. Polling the
    /// queue's bindings instead is the check that actually matches what a publish needs.
    /// </remarks>
    internal async Task WaitForBookingBindingsAsync(
        IReadOnlyCollection<string> routingKeys,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        HttpClient management = _management ??= CreateManagementClient();

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        while (true)
        {
            linked.Token.ThrowIfCancellationRequested();

            try
            {
                List<BindingInfo>? bindings = await management.GetFromJsonAsync<List<BindingInfo>>(
                    "/api/queues/%2F/booking/bindings", linked.Token);

                HashSet<string> bound = [.. (bindings ?? []).Select(binding => binding.RoutingKey)];

                if (routingKeys.All(bound.Contains))
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // The queue itself may not exist yet; the next iteration tries again.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), linked.Token);
        }
    }

    /// <summary>
    /// Waits until the given queue has no messages waiting and none unacknowledged — the fact that a message
    /// just published into it has been delivered, handled and acked by the consumer, without sleeping a
    /// guessed number of seconds.
    /// </summary>
    internal async Task WaitForQueueIdleAsync(string queue, TimeSpan timeout, CancellationToken cancellationToken)
    {
        HttpClient management = _management ??= CreateManagementClient();

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        while (true)
        {
            linked.Token.ThrowIfCancellationRequested();

            try
            {
                QueueInfo? info = await management.GetFromJsonAsync<QueueInfo>($"/api/queues/%2F/{queue}", linked.Token);

                if (info is { Messages: 0 })
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Not there yet; the next iteration tries again.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), linked.Token);
        }
    }

    private HttpClient CreateManagementClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new UriBuilder("http", _container.Hostname, _container.GetMappedPublicPort(ManagementPort)).Uri,
        };

        // The module's default credentials for the official image; the management API grants them the
        // administrator tag needed to read bindings.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("rabbitmq:rabbitmq")));

        return client;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _management?.Dispose();
        await _container.DisposeAsync();
    }

    private sealed record BindingInfo([property: JsonPropertyName("routing_key")] string RoutingKey);

    private sealed record QueueInfo([property: JsonPropertyName("messages")] int Messages);
}
