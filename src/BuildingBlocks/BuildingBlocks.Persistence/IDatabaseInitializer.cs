namespace QubicaCinema.BuildingBlocks.Persistence;

/// <summary>
/// Brings one database to the state the service expects before that service starts serving.
/// </summary>
/// <remarks>
/// Implemented by each service's Infrastructure project, which knows its own <c>DbContext</c>, and resolved
/// by the migration service, which knows none of them. The dependency points inwards: the migration service
/// depends on this abstraction, never the other way round.
/// </remarks>
public interface IDatabaseInitializer
{
    /// <summary>A name for logs and traces, for example <c>catalogdb</c>.</summary>
    string DatabaseName { get; }

    /// <summary>Applies every pending migration. Must be safe to run against an already current database.</summary>
    Task MigrateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Writes the demo data. Must be idempotent: it runs again on every start with a persistent volume.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken);
}
