using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace QubicaCinema.ServiceDefaults;

/// <summary>
/// Writes the body of a health-check response.
/// </summary>
/// <remarks>
/// Two writers on purpose. The detailed one names every dependency and repeats exception messages, which
/// can carry server names and connection details, so it is used in Development only; everywhere else the
/// response is the aggregate status and nothing more.
/// </remarks>
internal static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    internal static Task WriteStatusAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain";
        return context.Response.WriteAsync(report.Status.ToString());
    }

    internal static Task WriteDetailsAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var body = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration,
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message,
                tags = entry.Value.Tags,
            }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(body, SerializerOptions));
    }
}
