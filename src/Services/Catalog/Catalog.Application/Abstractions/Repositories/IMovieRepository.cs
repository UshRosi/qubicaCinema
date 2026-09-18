using QubicaCinema.Catalog.Domain.Movies;

namespace QubicaCinema.Catalog.Application.Abstractions.Repositories;

/// <summary>
/// Loads and stores <see cref="Movie"/> aggregates.
/// </summary>
/// <remarks>
/// Declared by the layer that uses it and implemented by the one that knows EF Core, so the use cases
/// depend on an interface they own. There is no <c>SaveChangesAsync</c> here: committing is
/// <see cref="BuildingBlocks.Domain.IUnitOfWork"/>'s job, because one use case may touch more than one
/// repository and they must commit together or not at all.
/// </remarks>
public interface IMovieRepository
{
    /// <summary>Loads a movie for editing, or null when there is none with that id.</summary>
    Task<Movie?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Stages a new movie. Nothing is written until the unit of work commits.</summary>
    void Add(Movie movie);

    /// <summary>
    /// Stages an edit, asserting the version the caller believes it is replacing.
    /// </summary>
    /// <remarks>
    /// The expected version is passed in rather than carried on the aggregate, which keeps the row version
    /// out of the model entirely: it is a fact about the row, not about the film. Infrastructure hands it
    /// to the store, and a mismatch becomes a
    /// <see cref="BuildingBlocks.Domain.ConcurrentModificationException"/> at commit time.
    /// </remarks>
    void Update(Movie movie, ReadOnlyMemory<byte> expectedVersion);
}
