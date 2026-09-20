namespace QubicaCinema.BuildingBlocks.Contracts.Catalog;

/// <summary>A screening was put on the programme.</summary>
/// <remarks>
/// Self-contained on purpose: it carries the auditorium's whole seat map, so a consumer needs no earlier
/// state and replaying the stream is trivially correct. The price is repeating the seat list for every
/// screening in the same room; the normalised alternative — an auditorium event plus a lean screening event —
/// would force the consumer to tolerate a screening that arrives before its auditorium.
/// </remarks>
/// <param name="ScreeningId">Catalog's id for the screening.</param>
/// <param name="MovieId">Catalog's id for the movie.</param>
/// <param name="MovieTitle">The movie's title, so a consumer needs no movies of its own.</param>
/// <param name="AuditoriumId">Catalog's id for the auditorium.</param>
/// <param name="AuditoriumName">The auditorium's name.</param>
/// <param name="StartsAt">When the audience is admitted.</param>
/// <param name="EndsAt">When the auditorium is free again, cleaning included.</param>
/// <param name="PriceAmount">What one seat costs.</param>
/// <param name="PriceCurrency">The ISO 4217 currency code of <paramref name="PriceAmount"/>.</param>
/// <param name="Seats">Every seat of the auditorium.</param>
public sealed record ScreeningScheduled(
    Guid ScreeningId,
    Guid MovieId,
    string MovieTitle,
    Guid AuditoriumId,
    string AuditoriumName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal PriceAmount,
    string PriceCurrency,
    IReadOnlyList<ScreeningSeat> Seats) : IntegrationEvent, IIntegrationEventContract
{
    /// <inheritdoc />
    public static string Manifest => CatalogEventNames.ScreeningScheduled;

    /// <inheritdoc />
    public override string EventName => Manifest;
}
