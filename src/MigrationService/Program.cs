using QubicaCinema.Bookings.Infrastructure;
using QubicaCinema.Catalog.Infrastructure;
using QubicaCinema.Identity.Persistence;
using QubicaCinema.MigrationService;
using QubicaCinema.ServiceDefaults;

// A plain host, not a web host: this process applies the schemas and exits, so it serves no requests.
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOptions<SeedOptions>()
    .Bind(builder.Configuration.GetSection(SeedOptions.SectionName))
    .ValidateDataAnnotations()
    // Fail while starting, not halfway through a migration.
    .ValidateOnStart();

// One line per service whose schema this process owns. Each Add*Infrastructure registers that service's
// IDatabaseInitializer, and the worker runs every one it finds without knowing what any of them are.
builder.Services.AddCatalogInfrastructure(builder.Configuration.RequireConnectionString(
    CatalogInfrastructureExtensions.DatabaseName));
builder.Services.AddBookingsInfrastructure(builder.Configuration.RequireConnectionString(
    BookingsInfrastructureExtensions.DatabaseName));

// Identity is the one service whose initializer is not inside AddXInfrastructure: the seed needs an
// administrator's credentials, which no API should have to be given, so it is registered here alone.
builder.Services.AddIdentityPersistence(builder.Configuration.RequireConnectionString(
    IdentityPersistenceExtensions.DatabaseName));
builder.Services.AddIdentityDatabaseInitializer(builder.Configuration.GetSection(IdentitySeedOptions.SectionName));

builder.Services.AddHostedService<DatabaseMigrationWorker>();

var host = builder.Build();
await host.RunAsync();
