namespace QubicaCinema.Catalog.Api.Screenings;

/// <summary>The body of <c>PUT /screenings/{id}</c>. The film and the room cannot change.</summary>
/// <param name="StartsAt">The new admission time.</param>
/// <param name="PriceAmount">The new seat price.</param>
/// <param name="PriceCurrency">The ISO-4217 code of that price.</param>
public sealed record RescheduleScreeningRequest(
    DateTimeOffset StartsAt,
    decimal PriceAmount,
    string PriceCurrency);
