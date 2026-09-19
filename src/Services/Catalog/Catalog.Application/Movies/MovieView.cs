using QubicaCinema.Catalog.Domain.Movies;

namespace QubicaCinema.Catalog.Application.Movies;

/// <summary>
/// A movie as the catalogue shows it.
/// </summary>
/// <remarks>
/// A read model, not the aggregate. The duration is flattened to whole minutes because that is how a
/// programme prints it and how a client will want to render it, and because a <see cref="TimeSpan"/> on the
/// wire is a formatting argument nobody needs to have.
/// </remarks>
public sealed record MovieView(
    Guid Id,
    string Title,
    string Description,
    int DurationMinutes,
    MovieGenre Genre,
    AgeRating AgeRating);
