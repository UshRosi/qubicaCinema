using Microsoft.Extensions.Options;

namespace QubicaCinema.Identity.UnitTests.Fixtures;

/// <summary>An options monitor whose value never changes, which is all a singleton under test needs from one.</summary>
internal sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;

    public T Get(string? name) => value;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
