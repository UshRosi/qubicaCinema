namespace QubicaCinema.BuildingBlocks.Domain;

/// <summary>A seat was described by a row or a number that no auditorium could have.</summary>
public sealed class InvalidSeatPositionException(string message) : DomainException(DomainErrorKind.Invalid, message);
