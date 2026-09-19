using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Screenings;

/// <summary>
/// A screening was added to the programme.
/// </summary>
/// <remarks>
/// A domain event, not an integration event: it stays inside Catalog. Chapter 3 turns these into the
/// outbox rows that Booking consumes, which is why the aggregate records them from the start — nothing in
/// the model has to change when messaging arrives.
/// </remarks>
public sealed record ScreeningScheduledDomainEvent(
    DateTimeOffset OccurredAt,
    Guid ScreeningId,
    Guid MovieId,
    Guid AuditoriumId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal PriceAmount,
    string PriceCurrency) : IDomainEvent;
