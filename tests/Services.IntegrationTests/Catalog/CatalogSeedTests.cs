using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QubicaCinema.BuildingBlocks.Persistence;
using QubicaCinema.Catalog.Infrastructure;
using QubicaCinema.Catalog.Infrastructure.Persistence;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Catalog;

/// <summary>
/// Proves that the demo catalogue is written exactly once when the seed has to be replayed after a transient
/// failure, whether the failure came before the data was saved or after it was committed.
/// </summary>
/// <remarks>
/// Each test seeds a database of its own on the shared server, because the seed skips a database that already
/// holds a film and the shared <c>catalogdb</c> is full of them. The service's own registration is used, with
/// one interceptor added, so the retrying execution strategy is the same one the migration service runs.
/// </remarks>
public sealed class CatalogSeedTests(CinemaFixture cinema)
{
    [Fact]
    public async Task A_save_that_fails_and_is_retried_seeds_the_catalogue_once()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FailingSaveInterceptor failingSave = new();
        await using ServiceProvider catalog = BuildCatalog(failingSave);

        await MigrateAndSeedAsync(catalog, beforeSeeding: failingSave.FailNextSave, cancellationToken);

        await ShouldHoldOneCatalogueAsync(catalog, cancellationToken);
    }

    [Fact]
    public async Task A_commit_whose_acknowledgement_is_lost_does_not_seed_the_catalogue_twice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LostCommitInterceptor lostCommit = new();
        await using ServiceProvider catalog = BuildCatalog(lostCommit);

        await MigrateAndSeedAsync(catalog, beforeSeeding: lostCommit.LoseNextCommit, cancellationToken);

        await ShouldHoldOneCatalogueAsync(catalog, cancellationToken);
    }

    private ServiceProvider BuildCatalog(IInterceptor fault)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(cinema.Clock);
        services.AddCatalogInfrastructure(cinema.ConnectionStringFor($"catalogseed_{Guid.NewGuid():N}"));
        services.ConfigureDbContext<CatalogDbContext>(options => options.AddInterceptors(fault));

        return services.BuildServiceProvider();
    }

    /// <summary>Runs the initializer the way the migration service does, arming the fault once the schema is in place.</summary>
    private static async Task MigrateAndSeedAsync(
        ServiceProvider catalog, Action beforeSeeding, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = catalog.CreateAsyncScope();
        IDatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

        await initializer.MigrateAsync(cancellationToken);
        beforeSeeding();
        await initializer.SeedAsync(cancellationToken);
    }

    private static async Task ShouldHoldOneCatalogueAsync(ServiceProvider catalog, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = catalog.CreateAsyncScope();
        CatalogDbContext context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        List<string> titles = await context.Movies.Select(movie => movie.Title).ToListAsync(cancellationToken);
        List<string> rooms = await context.Auditoriums.Select(auditorium => auditorium.Name).ToListAsync(cancellationToken);

        titles.ShouldNotBeEmpty();
        titles.ShouldBeUnique();
        rooms.ShouldNotBeEmpty();
        rooms.ShouldBeUnique();
    }
}
