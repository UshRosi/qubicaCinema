namespace QubicaCinema.AppHost;

/// <summary>
/// How the local development environment should behave, bound from the <c>Cinema</c> configuration section.
/// </summary>
/// <remarks>
/// The defaults favour a reviewer running the solution for the first time: every run starts from the same
/// empty databases and the same seed, so a failure is reproducible. A developer who prefers to keep data
/// between runs opts in through user secrets or <c>Cinema__UseVolumes=true</c>.
/// </remarks>
internal sealed record AppHostOptions
{
    internal const string SectionName = "Cinema";

    /// <summary>Keep the SQL Server data directory in a Docker volume instead of the container layer.</summary>
    public bool UseVolumes { get; init; }

    /// <summary>Keep the containers running after the AppHost exits, so the next start is fast.</summary>
    public bool PersistentContainers { get; init; }

    /// <summary>Let the migration service write the demo data a reviewer needs to try the API.</summary>
    public bool SeedData { get; init; } = true;
}
