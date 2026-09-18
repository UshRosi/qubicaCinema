namespace QubicaCinema.MigrationService;

/// <summary>Reads the settings this process cannot start without.</summary>
internal static class ConfigurationExtensions
{
    /// <summary>
    /// Returns a connection string, or fails immediately with a message that names what is missing.
    /// </summary>
    /// <remarks>
    /// The alternative is passing null into <c>UseSqlServer</c> and discovering it much later as an
    /// <c>ArgumentNullException</c> from inside the provider, with nothing to say which key was wrong.
    /// </remarks>
    internal static string RequireConnectionString(this IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name)
        ?? throw new InvalidOperationException(
            $"The connection string '{name}' is missing. The AppHost supplies it as ConnectionStrings__{name}.");
}
