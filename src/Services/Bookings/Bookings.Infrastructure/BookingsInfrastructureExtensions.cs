using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QubicaCinema.BuildingBlocks.Application.Idempotency;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.BuildingBlocks.Persistence;
using QubicaCinema.BuildingBlocks.Persistence.Idempotency;
using QubicaCinema.Bookings.Application.Abstractions.Queries;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Infrastructure.Persistence;
using QubicaCinema.Bookings.Infrastructure.Queries;
using QubicaCinema.Bookings.Infrastructure.Repositories;

namespace QubicaCinema.Bookings.Infrastructure;

/// <summary>Registers everything the Booking service needs in order to reach its database.</summary>
public static class BookingsInfrastructureExtensions
{
    /// <summary>The name of the connection string, and of the health check that watches it.</summary>
    public const string DatabaseName = "bookingdb";

    /// <summary>Wires up the Booking persistence layer against the given connection string.</summary>
    /// <remarks>The connection string is a parameter, for the same reasons as in Catalog.</remarks>
    public static IServiceCollection AddBookingsInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<BookingDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        // The factory form, so that IUnitOfWork is the very DbContext the repositories use.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<BookingDbContext>());

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IScreeningRepository, ScreeningRepository>();
        services.AddScoped<ISeatMapRepository, SeatMapRepository>();
        services.AddScoped<IBookingQueries, BookingQueries>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore<BookingDbContext>>();

        services.AddScoped<IDatabaseInitializer, BookingDatabaseInitializer>();

        services.TryAddSingleton(TimeProvider.System);

        services.AddHealthChecks()
            .AddDbContextCheck<BookingDbContext>(DatabaseName, tags: ["ready"]);

        return services;
    }
}
