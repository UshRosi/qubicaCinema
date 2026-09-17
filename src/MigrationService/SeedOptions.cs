using System.ComponentModel.DataAnnotations;

namespace QubicaCinema.MigrationService;

/// <summary>
/// Controls what the migration run does, bound from the <c>Seed</c> configuration section.
/// </summary>
internal sealed class SeedOptions
{
    internal const string SectionName = "Seed";

    /// <summary>Whether demo data is written after the migrations are applied.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// How long a single migration may take. The default is generous because a cold SQL Server container
    /// is still warming up when the first migration reaches it.
    /// </summary>
    [Range(30, 600)]
    public int TimeoutSeconds { get; init; } = 120;
}
