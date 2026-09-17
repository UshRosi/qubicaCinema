using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace QubicaCinema.ServiceDefaults;

/// <summary>
/// Decides whether the health endpoints are mapped at all.
/// </summary>
/// <remarks>
/// The endpoints are unauthenticated and, in Development, answer with the name and status of every
/// dependency. Outside Development that is information a public endpoint should not hand out, so mapping is
/// opt-in through <c>HealthChecks:Expose</c> — set it where a probe really reaches the process, such as a
/// Kubernetes liveness probe on an internal port.
/// </remarks>
internal static class HealthEndpointOptions
{
    private const string ExposeKey = "HealthChecks:Expose";

    internal static bool ShouldExpose(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() || configuration.GetValue<bool>(ExposeKey);
}
