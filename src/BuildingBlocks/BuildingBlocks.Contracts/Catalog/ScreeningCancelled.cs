namespace QubicaCinema.BuildingBlocks.Contracts.Catalog;

/// <summary>A screening was called off. Catalog keeps the screening; it is cancelled, never deleted.</summary>
/// <param name="ScreeningId">Catalog's id for the screening.</param>
/// <param name="Reason">Why it was called off, when a reason was given.</param>
public sealed record ScreeningCancelled(Guid ScreeningId, string? Reason = null)
    : IntegrationEvent, IIntegrationEventContract
{
    /// <inheritdoc />
    public static string Manifest => CatalogEventNames.ScreeningCancelled;

    /// <inheritdoc />
    public override string EventName => Manifest;
}
