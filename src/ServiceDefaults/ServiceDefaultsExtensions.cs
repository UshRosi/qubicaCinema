using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace QubicaCinema.ServiceDefaults;

/// <summary>
/// The cross-process defaults every QubicaCinema host applies: telemetry and health checks.
/// </summary>
/// <remarks>
/// Deliberately not here: service discovery and the standard resilience handler. The services never call
/// each other over HTTP — all integration goes through RabbitMQ — and YARP builds its own message invoker
/// per cluster, so both would be dead code that merely looks like protection.
/// </remarks>
public static class ServiceDefaultsExtensions
{
    private const string LiveTag = "live";
    private const string ReadyTag = "ready";

    /// <summary>
    /// Applies the shared defaults to any host.
    /// </summary>
    /// <remarks>
    /// Generic over <see cref="IHostApplicationBuilder"/> rather than taking a <c>WebApplicationBuilder</c>,
    /// so that the migration worker — a plain <see cref="Host"/>, not a web host — can call it too.
    /// </remarks>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        return builder;
    }

    /// <summary>
    /// Registers the liveness check. Services add their own readiness checks, tagged <c>ready</c>.
    /// </summary>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Liveness must never touch a dependency: if /alive pinged SQL Server, a thirty-second database
            // blip would make an orchestrator restart every otherwise healthy instance.
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [LiveTag]);

        return builder;
    }

    /// <summary>
    /// Maps <c>/alive</c> (liveness) and <c>/health</c> (readiness) when the host is allowed to expose them.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (!HealthEndpointOptions.ShouldExpose(app.Environment, app.Configuration))
        {
            return app;
        }

        Func<HttpContext, HealthReport, Task> responseWriter = app.Environment.IsDevelopment()
            ? HealthCheckResponseWriter.WriteDetailsAsync
            : HealthCheckResponseWriter.WriteStatusAsync;

        // "Is the process running?" — no dependency is consulted.
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(LiveTag),
            ResponseWriter = responseWriter,
        })
        .AllowAnonymous()
        .DisableRateLimiting();

        // "Can the process serve traffic?" — this is where dependencies belong.
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
            ResponseWriter = responseWriter,
        })
        .AllowAnonymous()
        .DisableRateLimiting();

        return app;
    }
}
