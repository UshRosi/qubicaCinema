namespace QubicaCinema.Catalog.Api.Screenings;

/// <summary>The body of <c>POST /screenings/{id}/cancellation</c>.</summary>
/// <param name="Reason">Why the screening was called off, if a reason is worth recording.</param>
public sealed record CancelScreeningRequest(string? Reason);
