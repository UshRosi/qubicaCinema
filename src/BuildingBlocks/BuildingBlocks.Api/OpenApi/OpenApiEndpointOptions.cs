using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace QubicaCinema.BuildingBlocks.Api.OpenApi;

/// <summary>
/// Decides whether a process publishes its OpenAPI document at all.
/// </summary>
/// <remarks>
/// The same switch as the health endpoints, deliberately: always in Development, and elsewhere only when
/// <c>OpenApi:Expose</c> is set. A schema lists every route, including the administrator's, so publishing it
/// is opt-in. It is a separate key rather than a reuse of <c>HealthChecks:Expose</c> because exposing a
/// probe and publishing a schema are different decisions that a deployment may want to make differently.
/// </remarks>
public static class OpenApiEndpointOptions
{
    private const string ExposeKey = "OpenApi:Expose";

    /// <summary>True when this process should map its OpenAPI document (and, at the gateway, the reference page).</summary>
    public static bool ShouldExpose(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() || configuration.GetValue<bool>(ExposeKey);
}
