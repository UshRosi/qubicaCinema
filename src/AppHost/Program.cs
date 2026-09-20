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
builder.AddProject<Projects.Catalog_Api>("catalog")
    .WithReference(catalogDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Bookings_Api>("booking")
    .WithReference(bookingDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
