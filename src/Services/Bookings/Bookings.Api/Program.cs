using System.Text.Json;
using System.Text.Json.Serialization;
using QubicaCinema.BuildingBlocks.Api.Endpoints;
using QubicaCinema.BuildingBlocks.Api.Errors;
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

builder.Services.AddProblemDetails();
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
builder.Services.AddStandInCurrentUser();

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

var api = app.MapGroup("/api/v1");

api.MapModule<BookingEndpoints>()
   .MapModule<SeatMapEndpoints>();

await app.RunAsync();

/// <summary>Named so that chapter 7's <c>WebApplicationFactory&lt;Program&gt;</c> has a type to point at.</summary>
public partial class Program;
