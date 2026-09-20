using FluentValidation;
using QubicaCinema.Bookings.Api.Bookings;

namespace QubicaCinema.Bookings.Api;

/// <summary>Registers what the HTTP layer of the Booking service needs.</summary>
internal static class BookingsApiExtensions
{
    /// <summary>Adds the request validators, as singletons for the same reason as in Catalog.</summary>
    /// <remarks>
    /// Only the root validator is registered: the ones for an item and for each kind of selection are
    /// composed inside it, so they are an implementation detail of how a booking request is checked.
    /// </remarks>
    internal static IServiceCollection AddBookingValidators(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        services.AddSingleton<IValidator<CreateBookingRequest>, CreateBookingRequestValidator>();

        return services;
    }
}
