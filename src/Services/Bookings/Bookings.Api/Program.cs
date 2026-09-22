using System.Text.Json;
using System.Text.Json.Serialization;
using QubicaCinema.BuildingBlocks.Api.Endpoints;
using QubicaCinema.BuildingBlocks.Api.Errors;
using QubicaCinema.BuildingBlocks.Api.OpenApi;
using QubicaCinema.BuildingBlocks.Authentication;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;
using QubicaCinema.Bookings.Api;
using QubicaCinema.Bookings.Api.Bookings;
using QubicaCinema.Bookings.Api.Screenings;
using QubicaCinema.Bookings.Application;
using QubicaCinema.Bookings.Application.IntegrationEvents;
using QubicaCinema.Bookings.Infrastructure;
using QubicaCinema.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// In every environment: a captive dependency is caught at startup rather than under load.
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

    // The seat selection is polymorphic on "mode". Without this, System.Text.Json accepts the
    // discriminator only as the first property, and a client that happens to serialise its fields in
    // another order gets a 400 for a request that is perfectly clear. Tolerant readers are a rule here.
    options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
});

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = AuthenticationProblemDetails.Describe);
// Exception handlers run in registration order; each declines what it does not recognise.
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddBookingsApplication();
builder.Services.AddBookingsInfrastructure(
    builder.Configuration.GetConnectionString(BookingsInfrastructureExtensions.DatabaseName)
    ?? throw new InvalidOperationException(
        "The connection string 'bookingdb' is missing. The AppHost supplies it as ConnectionStrings__bookingdb."));
builder.Services.AddBookingValidators();

// Booking's screenings and seats are not its own: Catalog owns them and announces every change. This queue is
// how they arrive. One handler per event; adding a fourth event means one more Subscribe line.
builder.Services.AddRabbitMqSubscriber(
    builder.Configuration.GetConnectionString(RabbitMqServiceCollectionExtensions.ConnectionName)
    ?? throw new InvalidOperationException(
        "The connection string 'rabbitmq' is missing. The AppHost supplies it as ConnectionStrings__rabbitmq."),
    queueName: "booking",
    subscriptions => subscriptions
        .Subscribe<ScreeningScheduled, ScreeningScheduledHandler>()
        .Subscribe<ScreeningRescheduled, ScreeningRescheduledHandler>()
        .Subscribe<ScreeningCancelled, ScreeningCancelledHandler>());

// The caller is whoever the validated token says, and the use cases see only ICurrentUser. Booking validates
// the token itself, as Catalog does: the gateway is not a trust boundary a service may rely on.
builder.Services.AddCinemaAuthentication(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddCinemaAuthorization();
builder.Services.AddCinemaCurrentUser();

// The service's own OpenAPI document. The gateway proxies it and serves one reference page for all three.
builder.Services.AddCinemaOpenApi(
    CinemaApiDocuments.Bookings,
    "QubicaCinema Bookings",
    "Seat availability and bookings: choose seats or ask for a number of seats, across one or several screenings, and cancel them again.");

var app = builder.Build();

app.UseExceptionHandler();
// The 401 and the 403 leave the security middleware with no body; this gives them the same ProblemDetails
// shape as every other error.
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapCinemaOpenApi();

var api = app.MapGroup("/api/v1");

api.MapModule<BookingEndpoints>()
   .MapModule<SeatMapEndpoints>();

await app.RunAsync();

/// <summary>Named so that chapter 7's <c>WebApplicationFactory&lt;Program&gt;</c> has a type to point at.</summary>
public partial class Program;
