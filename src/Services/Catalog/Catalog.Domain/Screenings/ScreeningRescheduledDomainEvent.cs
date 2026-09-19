using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Screenings;

/// <summary>A screening was moved or repriced.</summary>
public sealed record ScreeningRescheduledDomainEvent(
    DateTimeOffset OccurredAt,
    Guid ScreeningId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal PriceAmount,
    string PriceCurrency) : IDomainEvent;
