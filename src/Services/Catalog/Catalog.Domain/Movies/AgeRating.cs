namespace QubicaCinema.Catalog.Domain.Movies;

/// <summary>
/// How old a viewer must be, expressed in the neutral terms used across European ratings boards.
/// </summary>
/// <remarks>
/// National systems disagree on the exact ages, so the catalogue stores the category and leaves the
/// mapping to a local label — "VM14", "PG-13" — to whatever presents the catalogue.
/// </remarks>
public enum AgeRating
{
    /// <summary>Not rated yet.</summary>
    Unspecified = 0,

    /// <summary>Suitable for everyone.</summary>
    General,

    /// <summary>Parental guidance suggested.</summary>
    ParentalGuidance,

    /// <summary>Not suitable for younger teenagers.</summary>
    Teen,

    /// <summary>Adults only.</summary>
    Adult,
}
