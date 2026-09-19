using QubicaCinema.Catalog.Domain.Screenings;

namespace QubicaCinema.Catalog.Application.Screenings;

/// <summary>
/// A screening as the programme shows it.
/// </summary>
/// <remarks>
/// It carries the movie title and the auditorium name as well as their ids, because every client that
/// lists screenings needs them and a second round trip per row is the classic way to turn one query into
/// twenty-one. They are a snapshot of the query, not a copy kept anywhere.
/// <para>
/// <c>EndsAt</c> is when the auditorium is free again, cleaning time included — not when the credits roll.
/// </para>
/// </remarks>
public sealed record ScreeningView(
    Guid Id,
    Guid MovieId,
    string MovieTitle,
    Guid AuditoriumId,
    string AuditoriumName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal PriceAmount,
    string PriceCurrency,
    ScreeningStatus Status);
