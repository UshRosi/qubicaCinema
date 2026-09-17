using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace QubicaCinema.ServiceDefaults;

/// <summary>
/// Configures logging, metrics and tracing identically in every process of the solution.
/// </summary>
internal static class OpenTelemetryExtensions
{
    /// <summary>
    /// Paths that a probe or the dashboard polls constantly. Tracing them would bury the real requests.
    /// </summary>
    private static readonly string[] UninterestingPaths = ["/alive", "/health", "/metrics"];

    internal static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // One propagator for the whole solution: the trace context survives an HTTP hop and, once the
        // outbox restores it by hand, a RabbitMQ hop as well.
        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
        [
            new TraceContextPropagator(),
            new BaggagePropagator(),
        ]));

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(DiagnosticNames.Wildcard))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options => options.Filter = IsWorthTracing)
                .AddHttpClientInstrumentation()
                // Records the query text with its parameters redacted, which is what makes a slow query
                // readable without putting e-mail addresses or booking ids into exported spans. Capturing
                // parameter values is off by default and not publicly configurable in this release.
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource(DiagnosticNames.Wildcard));

        // Exporting is opt-in: with no collector configured the SDK still records, but nothing is shipped.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    private static bool IsWorthTracing(HttpContext context) =>
        !UninterestingPaths.Any(path => context.Request.Path.StartsWithSegments(path));
}
