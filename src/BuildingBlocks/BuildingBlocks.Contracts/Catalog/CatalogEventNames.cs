namespace QubicaCinema.BuildingBlocks.Contracts.Catalog;

/// <summary>
/// The wire names of the events Catalog publishes, in one place.
/// </summary>
/// <remarks>
/// Literals on purpose, never derived from a type name, a namespace or a naming convention: the name is what
/// is stored in the outbox and used as the routing key, so it must survive a renamed class or a moved file.
/// Kept together so that the whole vocabulary of the context can be read, and reviewed for accidental
/// changes, in one screen. Once published, a name is never edited; a change of meaning is a new
/// <c>…v2</c> entry beside the old one.
/// </remarks>
public static class CatalogEventNames
{
    /// <summary>A screening was put on the programme.</summary>
    public const string ScreeningScheduled = "catalog.screening-scheduled.v1";

    /// <summary>A screening was moved or repriced.</summary>
    public const string ScreeningRescheduled = "catalog.screening-rescheduled.v1";

    /// <summary>A screening was called off.</summary>
    public const string ScreeningCancelled = "catalog.screening-cancelled.v1";
}
