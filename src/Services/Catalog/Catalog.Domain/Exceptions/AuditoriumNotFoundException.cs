using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>No auditorium has this id.</summary>
public sealed class AuditoriumNotFoundException : DomainException
{
    /// <summary>Creates the exception from the id that was looked up.</summary>
    public AuditoriumNotFoundException(Guid auditoriumId)
        : base(DomainErrorKind.NotFound, $"No auditorium with id {auditoriumId} exists.") =>
        AddExtension("auditoriumId", auditoriumId);
}
