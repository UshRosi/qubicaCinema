namespace QubicaCinema.BuildingBlocks.Contracts;

/// <summary>
/// Gives an event type its wire name without an instance, so that a subscription can be registered for a
/// type alone.
/// </summary>
/// <remarks>
/// A static abstract member rather than an attribute or a naming convention: the compiler refuses an event
/// that forgets to name itself, and reading the name needs no reflection.
/// </remarks>
public interface IIntegrationEventContract
{
    /// <summary>The stable wire name, for example <c>catalog.screening-scheduled.v1</c>.</summary>
    static abstract string Manifest { get; }
}
