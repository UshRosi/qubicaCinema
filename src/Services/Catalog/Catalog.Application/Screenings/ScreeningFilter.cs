namespace QubicaCinema.Catalog.Application.Screenings;

/// <summary>
/// How to narrow and order a list of screenings.
/// </summary>
/// <remarks>
/// Typed fields and a closed sort enum, never a free-form <c>sort=</c> or <c>filter=</c> string. A string
/// there is an injection surface, it ties the API to column names, and it leaves the server unable to say
/// which sorts it can actually serve with an index. Everything a caller may ask for is listed here.
/// <para>
/// Cancelled screenings are hidden unless asked for: they are kept forever so that bookings still resolve,
/// and a programme that listed them would be showing films nobody can attend.
/// </para>
/// </remarks>
/// <param name="MovieId">Only screenings of this movie.</param>
/// <param name="AuditoriumId">Only screenings in this auditorium.</param>
/// <param name="From">Only screenings starting at or after this instant.</param>
/// <param name="To">Only screenings starting before this instant.</param>
/// <param name="Sort">How to order the result.</param>
/// <param name="IncludeCancelled">Whether cancelled screenings are listed too.</param>
public sealed record ScreeningFilter(
    Guid? MovieId = null,
    Guid? AuditoriumId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    ScreeningSort Sort = ScreeningSort.StartingSoonest,
    bool IncludeCancelled = false);
