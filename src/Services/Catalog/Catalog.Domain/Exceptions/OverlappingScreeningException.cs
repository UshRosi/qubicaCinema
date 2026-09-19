using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>
/// The auditorium is already showing something over part of the requested slot.
/// </summary>
/// <remarks>
/// Carries the screening it clashed with, so a client can show what is in the way without a second request.
/// </remarks>
public sealed class OverlappingScreeningException : DomainException
{
    /// <summary>Creates the exception from the auditorium and the screening already occupying the slot.</summary>
    public OverlappingScreeningException(Guid auditoriumId, Guid conflictingScreeningId)
        : base(
            DomainErrorKind.Conflict,
            $"Auditorium {auditoriumId} is already occupied by screening {conflictingScreeningId} during that slot.")
    {
        AddExtension("auditoriumId", auditoriumId);
        AddExtension("conflictingScreeningId", conflictingScreeningId);
    }
}
