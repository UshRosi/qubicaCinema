using System.Diagnostics;
using Microsoft.Extensions.Options;
using QubicaCinema.BuildingBlocks.Persistence;
using QubicaCinema.ServiceDefaults;

namespace QubicaCinema.MigrationService;

/// <summary>
/// Runs every registered <see cref="IDatabaseInitializer"/> once, then stops the host.
/// </summary>
internal sealed class DatabaseMigrationWorker(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    IOptions<SeedOptions> seedOptions,
    ILogger<DatabaseMigrationWorker> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new(DiagnosticNames.Root);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("Initialise databases");

        try
        {
            await InitialiseAllAsync(stoppingToken);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Database initialisation failed; no service will be started.");
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);

            // The non-zero exit code is what makes the orchestrator's WaitForCompletion fail fast, instead
            // of starting the services against a schema that was only half applied.
            Environment.ExitCode = 1;
        }
        finally
        {
            // This process has one job and is done, whether it succeeded or not.
            lifetime.StopApplication();
        }
    }

    private async Task InitialiseAllAsync(CancellationToken cancellationToken)
    {
        SeedOptions options = seedOptions.Value;

        // One scope for the run: the initializers own scoped DbContexts, and this worker is a singleton.
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IDatabaseInitializer[] initializers = [.. scope.ServiceProvider.GetServices<IDatabaseInitializer>()];

        if (initializers.Length == 0)
        {
            logger.LogWarning("No database initializer is registered; there is nothing to migrate.");
            return;
        }

        foreach (IDatabaseInitializer initializer in initializers)
        {
            using var activity = ActivitySource.StartActivity($"Initialise {initializer.DatabaseName}");
            using CancellationTokenSource timeout = CreateTimeout(options, cancellationToken);

            logger.LogInformation("Applying migrations to {Database}.", initializer.DatabaseName);
            await initializer.MigrateAsync(timeout.Token);

            if (options.Enabled)
            {
                logger.LogInformation("Seeding {Database}.", initializer.DatabaseName);
                await initializer.SeedAsync(timeout.Token);
            }
        }
    }

    private static CancellationTokenSource CreateTimeout(SeedOptions options, CancellationToken cancellationToken)
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        return timeout;
    }
}
