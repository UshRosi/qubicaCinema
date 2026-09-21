using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace QubicaCinema.BuildingBlocks.UnitTests.Authentication;

/// <summary>An environment of the test's choosing, for the rules that depend on where the process runs.</summary>
internal sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "Tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
