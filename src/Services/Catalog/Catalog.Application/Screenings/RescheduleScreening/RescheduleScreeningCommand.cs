namespace QubicaCinema.Catalog.Application.Screenings.RescheduleScreening;

/// <summary>Moves a screening or changes its price.</summary>
/// <param name="ScreeningId">Which screening.</param>
/// <param name="ExpectedVersion">The version the caller read, from its <c>If-Match</c>.</param>
/// <param name="StartsAt">The new admission time.</param>
/// <param name="PriceAmount">The new seat price.</param>
/// <param name="PriceCurrency">The ISO-4217 code of that price.</param>
public sealed record RescheduleScreeningCommand(
    Guid ScreeningId,
    ReadOnlyMemory<byte> ExpectedVersion,
    DateTimeOffset StartsAt,
    decimal PriceAmount,
    string PriceCurrency);
