using QubicaCinema.Catalog.Application.Screenings;

namespace QubicaCinema.Catalog.Api.Screenings;

/// <summary>The query string of <c>GET /screenings</c>.</summary>
/// <remarks>
/// Bound with <c>[AsParameters]</c>, and every field is typed: there is no <c>sort=</c> or <c>filter=</c>
/// string to parse. A caller can only ask for orderings the service has said it supports. The query names are
/// spelled out so the OpenAPI document advertises <c>movieId</c>, not the record's PascalCase <c>MovieId</c>.
/// </remarks>
/// <param name="MovieId">Only screenings of this film.</param>
/// <param name="AuditoriumId">Only screenings in this room.</param>
/// <param name="From">Only screenings starting at or after this instant.</param>
/// <param name="To">Only screenings starting before this instant.</param>
/// <param name="Sort">How to order the result.</param>
/// <param name="IncludeCancelled">Whether cancelled screenings appear too.</param>
public sealed record ScreeningFilterQuery(
    [FromQuery(Name = "movieId")] Guid? MovieId,
    [FromQuery(Name = "auditoriumId")] Guid? AuditoriumId,
    [FromQuery(Name = "from")] DateTimeOffset? From,
    [FromQuery(Name = "to")] DateTimeOffset? To,
    [FromQuery(Name = "sort")] ScreeningSort? Sort,
    [FromQuery(Name = "includeCancelled")] bool? IncludeCancelled)
{
    /// <summary>Converts the query string into the filter the use case understands.</summary>
    internal ScreeningFilter ToFilter() => new(
        MovieId,
        AuditoriumId,
        From,
        To,
        Sort ?? ScreeningSort.StartingSoonest,
        IncludeCancelled ?? false);
}
