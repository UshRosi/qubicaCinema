using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.Movies;

namespace QubicaCinema.Catalog.UnitTests.Movies;

public sealed class MovieTests
{
    [Fact]
    public void Should_trim_the_title_and_the_description()
    {
        var movie = Movie.Create("  Dune  ", "  Spice.  ", TimeSpan.FromMinutes(155), MovieGenre.ScienceFiction, AgeRating.Teen);

        movie.Title.ShouldBe("Dune");
        movie.Description.ShouldBe("Spice.");
    }

    [Fact]
    public void Should_accept_an_empty_description()
    {
        Movie.Create("Dune", string.Empty, TimeSpan.FromMinutes(155), MovieGenre.ScienceFiction, AgeRating.Teen)
            .Description.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_a_missing_title(string title)
    {
        Should.Throw<InvalidMovieDetailsException>(
            () => Movie.Create(title, "Spice.", TimeSpan.FromMinutes(155), MovieGenre.ScienceFiction, AgeRating.Teen));
    }

    [Fact]
    public void Should_reject_a_title_that_is_too_long()
    {
        Should.Throw<InvalidMovieDetailsException>(
            () => Movie.Create(new string('x', Movie.MaxTitleLength + 1), string.Empty, TimeSpan.FromMinutes(155), MovieGenre.Drama, AgeRating.General));
    }

    [Fact]
    public void Should_reject_a_duration_that_is_not_a_film()
    {
        Should.Throw<InvalidMovieDetailsException>(
            () => Movie.Create("Trailer", string.Empty, TimeSpan.Zero, MovieGenre.Drama, AgeRating.General));

        Should.Throw<InvalidMovieDetailsException>(
            () => Movie.Create("Marathon", string.Empty, Movie.MaximumDuration + TimeSpan.FromMinutes(1), MovieGenre.Drama, AgeRating.General));
    }

    [Fact]
    public void Should_keep_its_identity_when_its_details_are_corrected()
    {
        var movie = Movie.Create("Dune", "Spice.", TimeSpan.FromMinutes(155), MovieGenre.ScienceFiction, AgeRating.Teen);
        Guid originalId = movie.Id;

        movie.UpdateDetails("Dune: Part Two", "More spice.", TimeSpan.FromMinutes(166), MovieGenre.ScienceFiction, AgeRating.Teen);

        movie.Id.ShouldBe(originalId);
        movie.Title.ShouldBe("Dune: Part Two");
        movie.Duration.ShouldBe(TimeSpan.FromMinutes(166));
    }

    [Fact]
    public void Should_refuse_an_invalid_correction_without_changing_anything()
    {
        var movie = Movie.Create("Dune", "Spice.", TimeSpan.FromMinutes(155), MovieGenre.ScienceFiction, AgeRating.Teen);

        Should.Throw<InvalidMovieDetailsException>(
            () => movie.UpdateDetails(string.Empty, "x", TimeSpan.FromMinutes(90), MovieGenre.Drama, AgeRating.General));

        movie.Title.ShouldBe("Dune");
    }
}
