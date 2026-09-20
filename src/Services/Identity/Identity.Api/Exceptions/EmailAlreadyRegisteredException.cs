using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Identity.Api.Exceptions;

/// <summary>An account already exists for this email.</summary>
public sealed class EmailAlreadyRegisteredException()
    : DomainException(DomainErrorKind.Conflict, "An account with this email already exists.");
