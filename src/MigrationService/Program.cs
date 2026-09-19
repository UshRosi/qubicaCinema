using QubicaCinema.Bookings.Infrastructure;
using QubicaCinema.Catalog.Infrastructure;
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

builder.Services.AddHostedService<DatabaseMigrationWorker>();

var host = builder.Build();
await host.RunAsync();
