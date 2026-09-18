namespace QubicaCinema.Catalog.Application.Movies.GetMovie;

/// <summary>Asks for one film.</summary>
/// <param name="MovieId">Which film.</param>
public sealed record GetMovieQuery(Guid MovieId);
