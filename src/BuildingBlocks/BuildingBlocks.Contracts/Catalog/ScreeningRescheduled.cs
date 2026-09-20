namespace QubicaCinema.BuildingBlocks.Contracts.Catalog;

/// <summary>A screening was moved to another time or given another price.</summary>
/// <remarks>
/// Without this event a consumer would keep selling seats at the old time and the old price. The movie and
/// the auditorium never change: that would be a different screening.
/// </remarks>
/// <param name="ScreeningId">Catalog's id for the screening.</param>
/// <param name="StartsAt">The new admission time.</param>
/// <param name="EndsAt">When the auditorium is free again.</param>
/// <param name="PriceAmount">The new price of one seat.</param>
/// <param name="PriceCurrency">The ISO 4217 currency code of <paramref name="PriceAmount"/>.</param>
public sealed record ScreeningRescheduled(
    Guid ScreeningId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal PriceAmount,
    string PriceCurrency) : IntegrationEvent, IIntegrationEventContract
{
    /// <inheritdoc />
    public static string Manifest => CatalogEventNames.ScreeningRescheduled;

    /// <inheritdoc />
    public override string EventName => Manifest;
}
