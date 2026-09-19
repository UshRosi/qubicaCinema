using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>No screening has this id.</summary>
public sealed class ScreeningNotFoundException : DomainException
{
    /// <summary>Creates the exception from the id that was looked up.</summary>
    public ScreeningNotFoundException(Guid screeningId)
        : base(DomainErrorKind.NotFound, $"No screening with id {screeningId} exists.") =>
        AddExtension("screeningId", screeningId);
}
