using QubicaCinema.Catalog.Domain.Movies;

namespace QubicaCinema.Catalog.Application.Movies.CreateMovie;

/// <summary>Adds a film to the catalogue.</summary>
/// <param name="Title">The title as it will appear on the programme.</param>
/// <param name="Description">The synopsis.</param>
/// <param name="DurationMinutes">How long the film runs.</param>
/// <param name="Genre">The genre it is filed under.</param>
/// <param name="AgeRating">How old a viewer must be.</param>
public sealed record CreateMovieCommand(
    string Title,
    string Description,
    int DurationMinutes,
    MovieGenre Genre,
    AgeRating AgeRating);
