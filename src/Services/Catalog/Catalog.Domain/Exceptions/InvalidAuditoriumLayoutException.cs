using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>An auditorium was described with a grid that cannot be built.</summary>
public sealed class InvalidAuditoriumLayoutException(string message) : DomainException(DomainErrorKind.Invalid, message);
