using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Domain.Exceptions;

namespace QubicaCinema.Catalog.Domain.Movies;

/// <summary>
/// A film the cinema can show.
/// </summary>
/// <remarks>
/// An aggregate of its own, not part of a screening: the same movie is shown many times, and editing its
/// synopsis must not touch a single screening. A screening therefore holds only the movie's id, and copies
/// the duration it needs at the moment it is scheduled.
/// </remarks>
public sealed class Movie : AggregateRoot<Guid>
{
    /// <summary>The shortest film the catalogue accepts; below this it is a trailer.</summary>
    public static readonly TimeSpan MinimumDuration = TimeSpan.FromMinutes(1);

    /// <summary>The longest film the catalogue accepts. A typo of 6000 minutes is not a film.</summary>
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromHours(8);

    /// <summary>The longest title the catalogue stores.</summary>
    public const int MaxTitleLength = 200;

    /// <summary>The longest synopsis the catalogue stores.</summary>
    public const int MaxDescriptionLength = 2000;

    private Movie(Guid id, string title, string description, TimeSpan duration, MovieGenre genre, AgeRating ageRating)
        : base(id)
    {
        Title = title;
        Description = description;
        Duration = duration;
        Genre = genre;
        AgeRating = ageRating;
    }

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private Movie()
    {
        Title = string.Empty;
        Description = string.Empty;
    }

    /// <summary>The title as it appears on the programme.</summary>
    public string Title { get; private set; }

    /// <summary>The synopsis. May be empty, never null.</summary>
    public string Description { get; private set; }

    /// <summary>How long the film runs, excluding any cleaning time between screenings.</summary>
    public TimeSpan Duration { get; private set; }

    /// <summary>The genre it is filed under.</summary>
    public MovieGenre Genre { get; private set; }

    /// <summary>How old a viewer must be.</summary>
    public AgeRating AgeRating { get; private set; }

    /// <summary>Adds a film to the catalogue.</summary>
    /// <exception cref="InvalidMovieDetailsException">A detail is missing, too long, or not a plausible duration.</exception>
    public static Movie Create(
        string title,
        string description,
        TimeSpan duration,
        MovieGenre genre,
        AgeRating ageRating) =>
        new(
            Guid.CreateVersion7(),
            EnsureValidTitle(title),
            EnsureValidDescription(description),
            EnsureValidDuration(duration),
            genre,
            ageRating);

    /// <summary>Corrects the catalogue entry. The identity and every screening of the film are untouched.</summary>
    /// <exception cref="InvalidMovieDetailsException">A detail is missing, too long, or not a plausible duration.</exception>
    public void UpdateDetails(
        string title,
        string description,
        TimeSpan duration,
        MovieGenre genre,
        AgeRating ageRating)
    {
        Title = EnsureValidTitle(title);
        Description = EnsureValidDescription(description);
        Duration = EnsureValidDuration(duration);
        Genre = genre;
        AgeRating = ageRating;
    }

    private static string EnsureValidTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidMovieDetailsException("A movie must have a title.");
        }

        string trimmed = title.Trim();

        return trimmed.Length <= MaxTitleLength
            ? trimmed
            : throw new InvalidMovieDetailsException(
                $"A title may be at most {MaxTitleLength} characters, but was {trimmed.Length}.");
    }

    private static string EnsureValidDescription(string description)
    {
        string trimmed = (description ?? string.Empty).Trim();

        return trimmed.Length <= MaxDescriptionLength
            ? trimmed
            : throw new InvalidMovieDetailsException(
                $"A description may be at most {MaxDescriptionLength} characters, but was {trimmed.Length}.");
    }

    private static TimeSpan EnsureValidDuration(TimeSpan duration) =>
        duration >= MinimumDuration && duration <= MaximumDuration
            ? duration
            : throw new InvalidMovieDetailsException(
                $"A movie must run between {MinimumDuration.TotalMinutes:0} minute and "
                + $"{MaximumDuration.TotalHours:0} hours, but was {duration}.");
}
