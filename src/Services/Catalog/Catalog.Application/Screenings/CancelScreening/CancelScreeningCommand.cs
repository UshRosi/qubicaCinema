namespace QubicaCinema.Catalog.Application.Screenings.CancelScreening;

/// <summary>Calls a screening off.</summary>
/// <param name="ScreeningId">Which screening.</param>
/// <param name="Reason">Why, if a reason was given.</param>
public sealed record CancelScreeningCommand(Guid ScreeningId, string? Reason);
