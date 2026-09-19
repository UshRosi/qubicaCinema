using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QubicaCinema.Bookings.Application.Bookings.CancelBooking;
using QubicaCinema.Bookings.Application.Bookings.CancelBookingItem;
using QubicaCinema.Bookings.Application.Bookings.CreateBooking;
using QubicaCinema.Bookings.Application.Bookings.GetBooking;
using QubicaCinema.Bookings.Application.Bookings.GetBookings;
using QubicaCinema.Bookings.Application.Screenings.GetSeatAvailability;
using QubicaCinema.Bookings.Domain.SeatAllocation;

namespace QubicaCinema.Bookings.Application;

/// <summary>Registers the Booking use cases.</summary>
public static class BookingsApplicationExtensions
{
    /// <summary>Adds one registration per use case, by hand, as in Catalog.</summary>
    public static IServiceCollection AddBookingsApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateBookingHandler>();
        services.AddScoped<BookingPlacement>();
        services.AddScoped<CancelBookingHandler>();
        services.AddScoped<CancelBookingItemHandler>();
        services.AddScoped<GetBookingHandler>();
        services.AddScoped<GetBookingsHandler>();
        services.AddScoped<GetSeatAvailabilityHandler>();

        // A singleton: the strategy is a pure function over a seat map, with no state to share or leak.
        // Only if the API ever let a client choose a policy would it need keyed registrations and a
        // selector — and building that before anyone asks for it would be speculation.
        services.AddSingleton<ISeatAllocationStrategy, CenterFirstAdjacentSeatsStrategy>();

        // TryAdd, so that a test can substitute a FakeTimeProvider before this runs and keep it.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
