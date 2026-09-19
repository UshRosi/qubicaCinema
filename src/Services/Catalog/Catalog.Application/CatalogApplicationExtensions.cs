using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QubicaCinema.Catalog.Application.Auditoriums.CreateAuditorium;
using QubicaCinema.Catalog.Application.Auditoriums.GetAuditorium;
using QubicaCinema.Catalog.Application.Auditoriums.GetAuditoriums;
using QubicaCinema.Catalog.Application.Movies.CreateMovie;
using QubicaCinema.Catalog.Application.Movies.GetMovie;
using QubicaCinema.Catalog.Application.Movies.GetMovies;
using QubicaCinema.Catalog.Application.Movies.UpdateMovie;
using QubicaCinema.Catalog.Application.Screenings.CancelScreening;
using QubicaCinema.Catalog.Application.Screenings.GetScreening;
using QubicaCinema.Catalog.Application.Screenings.GetScreenings;
using QubicaCinema.Catalog.Application.Screenings.RescheduleScreening;
using QubicaCinema.Catalog.Application.Screenings.ScheduleScreening;

namespace QubicaCinema.Catalog.Application;

/// <summary>Registers the Catalog use cases.</summary>
public static class CatalogApplicationExtensions
{
    /// <summary>
    /// Adds one registration per use case.
    /// </summary>
    /// <remarks>
    /// One line per handler, by hand. Assembly scanning would save these lines and cost the two things
    /// they buy: the container is verified at build time by <c>ValidateOnBuild</c> because every service is
    /// named, and this list is the index of what the service can be asked to do. The concrete class is
    /// registered rather than <c>ICommandHandler&lt;,&gt;</c>, because a single-implementation generic
    /// interface in the container is indirection that only makes the endpoint signature longer.
    /// </remarks>
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateMovieHandler>();
        services.AddScoped<UpdateMovieHandler>();
        services.AddScoped<GetMoviesHandler>();
        services.AddScoped<GetMovieHandler>();

        services.AddScoped<CreateAuditoriumHandler>();
        services.AddScoped<GetAuditoriumsHandler>();
        services.AddScoped<GetAuditoriumHandler>();

        services.AddScoped<ScheduleScreeningHandler>();
        services.AddScoped<RescheduleScreeningHandler>();
        services.AddScoped<CancelScreeningHandler>();
        services.AddScoped<GetScreeningsHandler>();
        services.AddScoped<GetScreeningHandler>();

        // TryAdd, so that a test can substitute a FakeTimeProvider before this runs and keep it.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
