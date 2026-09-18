using FluentValidation;
using QubicaCinema.Catalog.Api.Auditoriums;
using QubicaCinema.Catalog.Api.Movies;
using QubicaCinema.Catalog.Api.Screenings;

namespace QubicaCinema.Catalog.Api;

/// <summary>Registers what the HTTP layer of the Catalog service needs.</summary>
internal static class CatalogApiExtensions
{
    /// <summary>
    /// Adds one validator per request contract, as singletons.
    /// </summary>
    /// <remarks>
    /// Singletons because a FluentValidation validator is a set of rules built once and then only read —
    /// it holds no per-request state. The library's own registration helper adds them as scoped, which
    /// builds a fresh rule set on every request for no benefit.
    /// </remarks>
    internal static IServiceCollection AddCatalogValidators(this IServiceCollection services)
    {
        // FluentValidation localises its built-in messages to the culture of the thread that formats them,
        // so the same request answers in Italian on one machine and English on another — and the solution
        // has one language by rule. Turning the language manager off pins every default message to English.
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        services.AddSingleton<IValidator<SaveMovieRequest>, SaveMovieRequestValidator>();
        services.AddSingleton<IValidator<CreateAuditoriumRequest>, CreateAuditoriumRequestValidator>();
        services.AddSingleton<IValidator<ScheduleScreeningRequest>, ScheduleScreeningRequestValidator>();
        services.AddSingleton<IValidator<RescheduleScreeningRequest>, RescheduleScreeningRequestValidator>();
        services.AddSingleton<IValidator<CancelScreeningRequest>, CancelScreeningRequestValidator>();

        return services;
    }
}
