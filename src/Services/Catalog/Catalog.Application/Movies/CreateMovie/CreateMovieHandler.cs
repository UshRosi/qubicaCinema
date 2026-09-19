using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Domain.Movies;

namespace QubicaCinema.Catalog.Application.Movies.CreateMovie;

/// <inheritdoc cref="CreateMovieCommand" />
/// <remarks>
/// Four lines of work: build the aggregate, stage it, commit, answer. Every rule about what a movie may be
/// is inside <see cref="Movie.Create"/>, so there is nothing here to validate and nothing to get wrong
/// twice.
/// </remarks>
public sealed class CreateMovieHandler(IMovieRepository movies, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateMovieCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> HandleAsync(CreateMovieCommand command, CancellationToken cancellationToken)
    {
        var movie = Movie.Create(
            command.Title,
            command.Description,
            TimeSpan.FromMinutes(command.DurationMinutes),
            command.Genre,
            command.AgeRating);

        movies.Add(movie);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return movie.Id;
    }
}
