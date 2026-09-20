using System.Text.Json;
using System.Text.Json.Serialization;
using QubicaCinema.BuildingBlocks.Api.Endpoints;
using QubicaCinema.BuildingBlocks.Api.Errors;
using QubicaCinema.BuildingBlocks.Authentication;
using QubicaCinema.Identity.Api;
using QubicaCinema.Identity.Api.Auth;
using QubicaCinema.Identity.Persistence;
using QubicaCinema.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// In every environment, not only Development: a singleton that captures a scoped service fails at startup.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.AddServiceDefaults();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
// Exception handlers run in registration order; each declines what it does not recognise.
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddIdentityPersistence(
    builder.Configuration.GetConnectionString(IdentityPersistenceExtensions.DatabaseName)
    ?? throw new InvalidOperationException(
        "The connection string 'identitydb' is missing. The AppHost supplies it as ConnectionStrings__identitydb."));

// Identity issues tokens and validates none, so it needs the options and their validation but not bearer
// authentication: a short signing key still stops this process at startup with a sentence naming the fix.
builder.Services.AddJwtOptions(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddIdentityUseCases();
builder.Services.AddIdentityValidators();

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

var api = app.MapGroup("/api/v1");

api.MapModule<AuthEndpoints>();

await app.RunAsync();

/// <summary>Named so that a <c>WebApplicationFactory&lt;Program&gt;</c> has a type to point at.</summary>
public partial class Program;
