namespace QubicaCinema.BuildingBlocks.Domain;

/// <summary>
/// Something that happened inside the model, recorded by the aggregate that caused it.
/// </summary>
/// <remarks>
/// A domain event stays inside its service. What crosses the wire to another service is an integration
/// event, a separate flat contract with a stable name — never one of these types.
/// </remarks>
public interface IDomainEvent
{
    /// <summary>When the event happened, taken from a <see cref="TimeProvider"/>, never from the clock.</summary>
    DateTimeOffset OccurredAt { get; }
}
