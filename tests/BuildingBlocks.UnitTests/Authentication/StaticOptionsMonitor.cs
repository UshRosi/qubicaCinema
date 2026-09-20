using Microsoft.Extensions.Options;

namespace QubicaCinema.BuildingBlocks.UnitTests.Authentication;

/// <summary>An options monitor whose value never changes, which is all these tests need from one.</summary>
internal sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;

    public T Get(string? name) => value;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
