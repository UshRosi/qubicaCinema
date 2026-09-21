using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Identity.Api.Exceptions;

/// <summary>Identity refused the new account for a reason the request's own validation could not see.</summary>
/// <remarks>
/// A password rule such as "needs a digit" is Identity's to enforce and configurable, so the validator does not
/// duplicate it; what Identity says is passed on, in the same <c>errors</c> extension the validator uses.
/// </remarks>
public sealed class RegistrationRejectedException : DomainException
{
    /// <summary>Creates the exception from the descriptions Identity gave.</summary>
    public RegistrationRejectedException(IReadOnlyCollection<string> errors)
        : base(DomainErrorKind.Invalid, "The account could not be created.") => AddExtension("errors", errors);
}
