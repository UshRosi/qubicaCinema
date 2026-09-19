namespace QubicaCinema.Catalog.Application.Movies.GetMovies;

/// <summary>Asks for one page of the film catalogue.</summary>
/// <param name="Skip">How many films to skip.</param>
/// <param name="Take">How many films to return.</param>
public sealed record GetMoviesQuery(int Skip, int Take);
