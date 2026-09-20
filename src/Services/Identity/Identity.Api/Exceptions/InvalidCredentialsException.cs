using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Identity.Api.Exceptions;

/// <summary>The email and password do not identify a user.</summary>
/// <remarks>
/// One exception, one message, for an unknown email and for a wrong password alike. Telling the two apart would
/// let anyone learn which addresses have an account just by trying them.
/// </remarks>
public sealed class InvalidCredentialsException()
    : DomainException(DomainErrorKind.Unauthenticated, "The email or password is incorrect.");
