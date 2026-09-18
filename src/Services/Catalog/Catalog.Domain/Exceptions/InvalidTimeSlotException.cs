using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>A time slot ended before it started, or lasted no time at all.</summary>
public sealed class InvalidTimeSlotException(string message) : DomainException(DomainErrorKind.Invalid, message);
