using System.Text.Json.Serialization;
using QubicaCinema.BuildingBlocks.Api.Endpoints;
using QubicaCinema.BuildingBlocks.Api.Errors;
using QubicaCinema.Catalog.Api;
using QubicaCinema.Catalog.Api.Auditoriums;
using QubicaCinema.Catalog.Api.Movies;
using QubicaCinema.Catalog.Api.Screenings;
using QubicaCinema.Catalog.Application;
using QubicaCinema.Catalog.Infrastructure;
using QubicaCinema.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// In every environment, not only Development. These two checks catch a captive dependency — a singleton
// holding a scoped DbContext — at startup, where it is one clear message, instead of at run time, where it
// is an intermittent "a second operation was started on this context" under load.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.AddServiceDefaults();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

    // Enums travel as their names. A client reading "Cancelled" cannot misread it, and inserting a value
    // into the enum later cannot silently renumber what older clients already understand.
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());

    // UnmappedMemberHandling stays at its default of Skip. Tolerant readers are a rule of this solution:
    // from chapter 3 the same convention carries event contracts, where rejecting an unknown property
    // would mean a new field in a publisher takes down every consumer that has not been redeployed.
});

// One exception handler for the whole service; no endpoint and no use case contains a try/catch.
builder.Services.AddProblemDetails();
// Exception handlers run in registration order; each declines what it does not recognise.
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(
    builder.Configuration.GetConnectionString(CatalogInfrastructureExtensions.DatabaseName)
    ?? throw new InvalidOperationException(
        "The connection string 'catalogdb' is missing. The AppHost supplies it as ConnectionStrings__catalogdb."));
builder.Services.AddCatalogValidators();

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

// Versioned from the first commit: adding /v1 later would itself be the breaking change it is meant to
// avoid, and the group costs one line.
var api = app.MapGroup("/api/v1");

api.MapModule<MovieEndpoints>()
   .MapModule<AuditoriumEndpoints>()
   .MapModule<ScreeningEndpoints>();

await app.RunAsync();

/// <summary>
/// Named so that chapter 7's <c>WebApplicationFactory&lt;Program&gt;</c> has a type to point at: a
/// top-level program's entry class is internal, and the factory needs it to be reachable.
/// </summary>
public partial class Program;
