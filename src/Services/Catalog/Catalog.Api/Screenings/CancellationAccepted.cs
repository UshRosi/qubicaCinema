namespace QubicaCinema.Catalog.Api.Screenings;

/// <summary>What a successful cancellation answers with.</summary>
/// <param name="ScreeningId">The screening that is now cancelled, whether or not it already was.</param>
public sealed record CancellationAccepted(Guid ScreeningId);
