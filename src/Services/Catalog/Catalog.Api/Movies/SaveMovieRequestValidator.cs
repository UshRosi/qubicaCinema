using FluentValidation;
using QubicaCinema.Catalog.Domain.Movies;

namespace QubicaCinema.Catalog.Api.Movies;

/// <summary>
/// Checks what can be decided from the body alone.
/// </summary>
/// <remarks>
/// The same rules live in <see cref="Movie.Create"/>, and that is not duplication with a different purpose:
/// the aggregate is the guarantee and answers with an exception, while this answers the caller with every
/// problem at once rather than only the first one the model happened to hit. The bounds are read off the
/// model so the two cannot drift apart.
/// </remarks>
internal sealed class SaveMovieRequestValidator : AbstractValidator<SaveMovieRequest>
{
    public SaveMovieRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(Movie.MaxTitleLength);

        RuleFor(request => request.Description)
            .MaximumLength(Movie.MaxDescriptionLength);

        RuleFor(request => request.DurationMinutes)
            .InclusiveBetween((int)Movie.MinimumDuration.TotalMinutes, (int)Movie.MaximumDuration.TotalMinutes);

        RuleFor(request => request.Genre).IsInEnum();
        RuleFor(request => request.AgeRating).IsInEnum();
    }
}
