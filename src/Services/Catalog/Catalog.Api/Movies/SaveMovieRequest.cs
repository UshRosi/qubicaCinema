using QubicaCinema.Catalog.Domain.Movies;

namespace QubicaCinema.Catalog.Api.Movies;

/// <summary>
/// The body of <c>POST /movies</c> and <c>PUT /movies/{id}</c>.
/// </summary>
/// <remarks>
/// One contract for both, because a film created and a film corrected carry exactly the same fields. Two
/// near-identical records would drift apart at the first change to either.
/// </remarks>
/// <param name="Title">The title as it appears on the programme.</param>
/// <param name="Description">The synopsis. May be empty.</param>
/// <param name="DurationMinutes">How long the film runs, excluding cleaning time.</param>
/// <param name="Genre">The genre it is filed under.</param>
/// <param name="AgeRating">How old a viewer must be.</param>
public sealed record SaveMovieRequest(
    string Title,
    string? Description,
    int DurationMinutes,
    MovieGenre Genre,
    AgeRating AgeRating);
