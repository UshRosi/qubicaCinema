using QubicaCinema.Catalog.Domain.Movies;

namespace QubicaCinema.Catalog.Application.Movies.UpdateMovie;

/// <summary>Corrects a catalogue entry.</summary>
/// <param name="MovieId">Which film.</param>
/// <param name="ExpectedVersion">The version the caller read, from its <c>If-Match</c>.</param>
/// <param name="Title">The corrected title.</param>
/// <param name="Description">The corrected synopsis.</param>
/// <param name="DurationMinutes">The corrected running time.</param>
/// <param name="Genre">The corrected genre.</param>
/// <param name="AgeRating">The corrected age rating.</param>
public sealed record UpdateMovieCommand(
    Guid MovieId,
    ReadOnlyMemory<byte> ExpectedVersion,
    string Title,
    string Description,
    int DurationMinutes,
    MovieGenre Genre,
    AgeRating AgeRating);
