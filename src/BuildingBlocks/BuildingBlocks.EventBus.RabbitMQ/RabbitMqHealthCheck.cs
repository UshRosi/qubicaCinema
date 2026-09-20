using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// Reports whether the service can reach the broker.
/// </summary>
/// <remarks>
/// Registered with a failure status of <see cref="HealthStatus.Degraded"/>, never <c>Unhealthy</c>. The broker
/// being down must not make a service unready: Catalog still accepts writes, which the outbox holds until the
/// broker returns, and Booking still answers reads. It is worth showing on the dashboard, and not worth
/// taking a service out of rotation for.
/// </remarks>
internal sealed class RabbitMqHealthCheck(RabbitMqConnectionProvider connections) : IHealthCheck
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (connections.IsConnected)
        {
            return HealthCheckResult.Healthy();
        }

        // A publisher that has had nothing to send has not connected yet; try, but do not let a probe wait
        // through the provider's whole retry schedule.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ConnectTimeout);

        try
        {
            IConnection connection = await connections.GetConnectionAsync(timeout.Token);

            // The provider hands out its connection even while the client is recovering it, so having one
            // says nothing about whether it works right now.
            return connection.IsOpen
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus, "RabbitMQ is unreachable; reconnecting.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "RabbitMQ is not reachable.", exception);
        }
    }
}
