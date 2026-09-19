namespace QubicaCinema.Catalog.Api.Screenings;

/// <summary>The body of <c>POST /screenings</c>.</summary>
/// <param name="MovieId">The film to show.</param>
/// <param name="AuditoriumId">The room to show it in.</param>
/// <param name="StartsAt">When the audience is admitted.</param>
/// <param name="PriceAmount">What one seat costs.</param>
/// <param name="PriceCurrency">The ISO-4217 code of that price, for example <c>EUR</c>.</param>
public sealed record ScheduleScreeningRequest(
    Guid MovieId,
    Guid AuditoriumId,
    DateTimeOffset StartsAt,
    decimal PriceAmount,
    string PriceCurrency);
