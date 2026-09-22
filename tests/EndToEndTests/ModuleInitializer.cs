using System.Runtime.CompilerServices;

namespace QubicaCinema.EndToEndTests;

/// <summary>Sets the two environment variables every Aspire testing host in this assembly needs, before any of them start.</summary>
internal static class ModuleInitializer
{
    /// <summary>
    /// Runs once, as the assembly loads — before <see cref="AssemblyFixtureAttribute{TFixture}"/> builds
    /// <c>CinemaAppFixture</c>, which is the earliest a normal setup hook could run.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Each Aspire testing host otherwise installs an inotify watch per configuration file it loads
        // across all five processes it starts; on a Linux CI runner that exhausts the per-user watch limit
        // long before the suite finishes.
        Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");

        // The AppHost's resources talk to each other over plain http, which is fine inside the orchestrator's
        // own network but is otherwise a guard Aspire enforces; the testing host needs it relaxed the same
        // way `dotnet run` does under its own launch profile.
        Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
    }
}
