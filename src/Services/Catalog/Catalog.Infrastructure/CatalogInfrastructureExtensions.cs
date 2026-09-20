using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.BuildingBlocks.Persistence;
using QubicaCinema.BuildingBlocks.Persistence.Outbox;
using QubicaCinema.Catalog.Application.Abstractions.Queries;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Infrastructure.Persistence;
using QubicaCinema.Catalog.Infrastructure.Queries;
using QubicaCinema.Catalog.Infrastructure.Repositories;

namespace QubicaCinema.Catalog.Infrastructure;

/// <summary>Registers everything the Catalog service needs in order to reach its database.</summary>
public static class CatalogInfrastructureExtensions
{
    /// <summary>The name of the connection string, and of the health check that watches it.</summary>
    public const string DatabaseName = "catalogdb";

    /// <summary>
    /// Wires up the Catalog persistence layer against the given connection string.
    /// </summary>
    /// <remarks>
    /// The connection string is a parameter, not something this method fishes out of
    /// <c>IConfiguration</c>. A composition root that reads configuration behind the caller's back cannot
    /// be used from a test, from a second host, or with a value that came from anywhere else — and the
    /// migration service and the API both call this with the same key read at their own level.
    /// </remarks>
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                // A containerised SQL Server drops connections while it starts, and a transient failure on
                // the first query is the normal case here, not an exceptional one.
                sql.EnableRetryOnFailure();

                // The migrations live beside the DbContext in this assembly, so no MigrationsAssembly call
                // is needed — the default is already right, and saying so is worth a line.
            }));

        // The two-argument overload — AddScoped<IUnitOfWork, CatalogDbContext>() — would register a second
        // DbContext for the same scope. The repositories would track their changes on one instance and the
        // commit would run on the other, which saves nothing and reports success. This factory form returns
        // the very instance the repositories are using.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CatalogDbContext>());

        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<IAuditoriumRepository, AuditoriumRepository>();
        services.AddScoped<IScreeningRepository, ScreeningRepository>();
        services.AddScoped<IMovieQueries, MovieQueries>();
        services.AddScoped<IAuditoriumQueries, AuditoriumQueries>();
        services.AddScoped<IScreeningQueries, ScreeningQueries>();

        // The migration service resolves this without knowing anything about Catalog.
        services.AddScoped<IDatabaseInitializer, CatalogDatabaseInitializer>();

        // The seed needs a clock. TryAdd, because the API registers the same one from Application and
        // whichever call comes first must win without the other overwriting it.
        services.TryAddSingleton(TimeProvider.System);

        // Readiness, not liveness: a service whose database is unreachable cannot serve requests, but it
        // should not be restarted for it.
        services.AddHealthChecks()
            .AddDbContextCheck<CatalogDbContext>(DatabaseName, tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Adds the publisher that sends what Catalog wrote to its outbox to the event bus.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="AddCatalogInfrastructure"/> because only the API publishes. The migration
    /// service writes outbox rows when it seeds the catalogue, and must not try to publish them: it has no
    /// bus, and the API drains the rows when it starts. Integration tests pass <c>false</c> and pump
    /// <see cref="IOutboxProcessor"/> by hand.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="runPublisher">Whether to start the publishing loop.</param>
    public static IServiceCollection AddCatalogOutboxPublisher(this IServiceCollection services, bool runPublisher = true) =>
        services.AddOutboxPublisher<CatalogDbContext>(runPublisher);
}
