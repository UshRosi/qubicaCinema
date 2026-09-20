using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QubicaCinema.BuildingBlocks.Persistence.Outbox;

/// <summary>
/// Runs the outbox processor for as long as the host lives.
/// </summary>
/// <remarks>
/// A singleton that owns nothing scoped: it takes the scope factory and opens a scope per pass, because the
/// <c>DbContext</c> behind the processor is scoped and a singleton holding one would be a captive dependency.
/// It reads <see cref="IOptionsMonitor{TOptions}"/>, since a singleton cannot use the snapshot variant, and
/// sleeps through the <see cref="TimeProvider"/> so that a test can drive the loop with a fake clock.
/// </remarks>
public sealed class OutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxPublisherBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                bool moreWaiting = await RunOnePassAsync(stoppingToken);

                if (!moreWaiting)
                {
                    await Task.Delay(options.CurrentValue.PollingInterval, clock, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping; that is the loop ending, not an error.
        }
    }

    private async Task<bool> RunOnePassAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

            OutboxBatchResult result = await processor.ProcessBatchAsync(stoppingToken);

            return result.MoreWaiting;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The database may be starting or restarting. The publisher must outlive that: a background
            // service that throws stops the whole host, and the outbox exists so that a bad hour is harmless.
            logger.LogError(exception, "The outbox publisher could not complete a pass; it will try again.");

            return false;
        }
    }
}
