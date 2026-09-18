using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>A movie was described without the details every movie must have.</summary>
public sealed class InvalidMovieDetailsException(string message) : DomainException(DomainErrorKind.Invalid, message);
