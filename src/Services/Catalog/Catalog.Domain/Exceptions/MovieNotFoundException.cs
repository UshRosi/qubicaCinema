using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>No movie has this id.</summary>
public sealed class MovieNotFoundException : DomainException
{
    /// <summary>Creates the exception from the id that was looked up.</summary>
    public MovieNotFoundException(Guid movieId)
        : base(DomainErrorKind.NotFound, $"No movie with id {movieId} exists.") => AddExtension("movieId", movieId);
}
