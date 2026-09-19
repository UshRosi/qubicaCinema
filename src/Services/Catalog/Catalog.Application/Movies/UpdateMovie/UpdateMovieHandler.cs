using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.Movies;

namespace QubicaCinema.Catalog.Application.Movies.UpdateMovie;

/// <inheritdoc cref="UpdateMovieCommand" />
public sealed class UpdateMovieHandler(IMovieRepository movies, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateMovieCommand, Guid>
{
    /// <inheritdoc />
    /// <exception cref="MovieNotFoundException">No film has that id.</exception>
    /// <exception cref="ConcurrentModificationException">Someone else edited it first.</exception>
    public async Task<Guid> HandleAsync(UpdateMovieCommand command, CancellationToken cancellationToken)
    {
        Movie movie = await movies.FindAsync(command.MovieId, cancellationToken)
                      ?? throw new MovieNotFoundException(command.MovieId);

        movie.UpdateDetails(
            command.Title,
            command.Description,
            TimeSpan.FromMinutes(command.DurationMinutes),
            command.Genre,
            command.AgeRating);

        movies.Update(movie, command.ExpectedVersion);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return movie.Id;
    }
}
