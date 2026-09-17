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

// Each service registers its own IDatabaseInitializer here from chapter 1 onwards. Until then the worker
// finds none and exits successfully, which is the correct behaviour for an empty solution.
builder.Services.AddHostedService<DatabaseMigrationWorker>();

var host = builder.Build();
await host.RunAsync();
