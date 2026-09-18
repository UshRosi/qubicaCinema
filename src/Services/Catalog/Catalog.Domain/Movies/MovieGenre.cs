namespace QubicaCinema.Catalog.Domain.Movies;

/// <summary>
/// The genre a movie is filed under in the catalogue.
/// </summary>
/// <remarks>
/// A closed set rather than free text: it cannot be misspelled, it serialises as a readable string, and a
/// client can build a filter from it. Real cinemas let a film carry several genres, which is a many-to-many
/// table and a richer editorial model; that is deliberately out of scope for a seat-booking exercise.
/// </remarks>
public enum MovieGenre
{
    /// <summary>Not classified.</summary>
    Unspecified = 0,
    Action,
    Adventure,
    Animation,
    Comedy,
    Documentary,
    Drama,
    Fantasy,
    Horror,
    Romance,
    ScienceFiction,
    Thriller,
}
