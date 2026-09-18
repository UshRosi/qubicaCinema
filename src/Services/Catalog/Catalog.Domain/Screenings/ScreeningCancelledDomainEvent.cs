using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Screenings;

/// <summary>A screening was called off. The screening itself is kept.</summary>
public sealed record ScreeningCancelledDomainEvent(
    DateTimeOffset OccurredAt,
    Guid ScreeningId,
    string? Reason) : IDomainEvent;
