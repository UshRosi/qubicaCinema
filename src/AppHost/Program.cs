using Microsoft.Extensions.Configuration;
using QubicaCinema.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var options = builder.Configuration.GetSection(AppHostOptions.SectionName).Get<AppHostOptions>()
              ?? new AppHostOptions();

var containerLifetime = options.PersistentContainers ? ContainerLifetime.Persistent : ContainerLifetime.Session;

// Both parameters are read from the Parameters section of configuration, so a developer overrides them
// with user secrets or an environment variable rather than by editing code. The signing key is unused
// until chapter 5, where Identity signs with it and every validator is handed the same value.
var sqlPassword = builder.AddParameter("sql-password", secret: true);
builder.AddParameter("jwt-signing-key", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithLifetime(containerLifetime);

if (options.UseVolumes)
{
    sql = sql.WithDataVolume();
}

// The resource names are the connection-string keys: WithReference hands a service
// ConnectionStrings__catalogdb, which is the plain configuration key the service reads.
var identityDb = sql.AddDatabase("identitydb");
var catalogDb = sql.AddDatabase("catalogdb");
var bookingDb = sql.AddDatabase("bookingdb");

// The management UI is linked from the dashboard, so a reviewer can watch queues drain while seats are
// booked. It costs about 100 MB of image and is worth it for an event-driven exercise.
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithLifetime(containerLifetime);

// Schema and seed data are applied by one process that runs to completion. Every service will wait for it,
// so no service ever starts against a half-built schema.
var migrations = builder.AddProject<Projects.MigrationService>("migrations")
    .WithReference(identityDb).WaitFor(identityDb)
    .WithReference(catalogDb).WaitFor(catalogDb)
    .WithReference(bookingDb).WaitFor(bookingDb)
    // A worker reads DOTNET_ENVIRONMENT, not ASPNETCORE_ENVIRONMENT, so without this line it would run as
    // Production and quietly lose the Development-only behaviour of the shared telemetry defaults.
    .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("Seed__Enabled", options.SeedData.ToString());

// WaitForCompletion, not WaitFor: the migration service is a task that ends, and the API must not start
// against a schema that is still being applied. A non-zero exit code from it stops the API from starting
// at all, which is the behaviour wanted — a service on a half-built schema fails in far stranger ways.
// WithReference(rabbitmq) hands each service ConnectionStrings__rabbitmq, the same plain key the code reads
// under any other deployment. WaitFor, not WaitForCompletion: the broker keeps running, and a service that
// starts before it is ready would only spend its first connection attempts failing.
//
// HealthChecks__Expose: the gateway's active health check probes /health every ten seconds. ServiceDefaults
// maps that endpoint in Development or when HealthChecks:Expose is set, and setting it here means the probe
// behaves the same wherever this runs, instead of getting a 404 in Production and marking a healthy service dead.
var catalog = builder.AddProject<Projects.Catalog_Api>("catalog")
    .WithReference(catalogDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health")
    .WithEnvironment("HealthChecks__Expose", "true");

var booking = builder.AddProject<Projects.Bookings_Api>("booking")
    .WithReference(bookingDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health")
    .WithEnvironment("HealthChecks__Expose", "true");

// The one address a client needs. No WithReference: that would inject service-discovery keys the gateway does
// not read. And no WaitFor: the gateway is deliberately independent of the services it fronts. That is the same
// property that keeps it healthy, and /api/v1/bookings answering, while Catalog is down. Until a service is up
// its cluster has no healthy destination and its routes answer 503, which is the honest answer and the one a
// client sees in production too.
builder.AddProject<Projects.Gateway>("gateway")
    .WithProxyDestination("catalog", catalog)
    .WithProxyDestination("booking", booking)
    // The gateway's own health only. It never aggregates downstream health: that is what the per-cluster
    // active checks are for, and they degrade one route rather than the whole entry point.
    .WithHttpHealthCheck("/health")
    // The only resource a client outside the orchestrator is meant to reach.
    .WithExternalHttpEndpoints();

builder.Build().Run();
