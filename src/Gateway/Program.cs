using QubicaCinema.Gateway;
using QubicaCinema.Gateway.Errors;
using QubicaCinema.Gateway.RateLimiting;
using QubicaCinema.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// In every environment, as in both services: a captive dependency is one clear message at startup rather
// than an intermittent failure under load.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.AddServiceDefaults();

// One customiser for every error this process produces itself. It never touches a proxied response: a 404
// from Catalog arrives with Catalog's own ProblemDetails body and passes through untouched.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = GatewayProblems.Describe);

// The shared defaults subscribe to QubicaCinema.* only. YARP publishes its spans and meters under its own
// name, and without this the gateway would be a blank segment in the middle of every trace.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(GatewayDiagnostics.ReverseProxy))
    .WithMetrics(metrics => metrics.AddMeter(GatewayDiagnostics.ReverseProxy));

// Routes, clusters, timeouts and probes are data, not code. The AppHost overrides exactly two strings.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection(ReverseProxyConfig.SectionName));

builder.Services.AddGatewayRateLimiting(builder.Configuration);

// A YARP route's "Timeout" is ASP.NET Core's request-timeout metadata. Without AddRequestTimeouts here and
// UseRequestTimeouts below, the metadata is emitted and silently ignored.
builder.Services.AddRequestTimeouts();

var app = builder.Build();

// Outermost, so a fault anywhere below still leaves as ProblemDetails rather than as an empty 500.
app.UseExceptionHandler();

// Every status the gateway produces on its own (404 for an unrouted path, 429 from the limiter, 502, 503
// and 504 from the forwarder) leaves the pipeline with no body, and this is the single place that gives it
// one. A client therefore parses one error shape whether the answer came from here or from a service.
app.UseStatusCodePages();

// Before the timeout middleware, because refusing a request is cheaper than starting a time budget for it.
// WebApplication puts routing ahead of all user middleware, so the endpoint and its metadata are already
// selected here. That is what lets DisableRateLimiting on /health and /alive take effect, and what lets a
// YARP route's RateLimiterPolicy resolve.
app.UseRateLimiter();

app.UseRequestTimeouts();

// The gateway's own liveness and readiness. They answer for this process only: if a dead Catalog made the
// gateway unhealthy, a load balancer would pull the gateway out and take /api/v1/bookings down with it.
app.MapDefaultEndpoints();

app.MapReverseProxy();

await app.RunAsync();

/// <summary>
/// Named so that the gateway's tests have a type for <c>WebApplicationFactory&lt;Program&gt;</c>: a
/// top-level program's entry class is internal, and the factory needs it to be reachable.
/// </summary>
public partial class Program;
