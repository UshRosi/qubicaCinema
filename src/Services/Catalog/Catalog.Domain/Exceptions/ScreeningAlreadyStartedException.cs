using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>The screening has already begun, so it can no longer be changed or cancelled.</summary>
public sealed class ScreeningAlreadyStartedException : DomainException
{
    /// <summary>Creates the exception from the screening and the time it started.</summary>
    public ScreeningAlreadyStartedException(Guid screeningId, DateTimeOffset startedAt)
        : base(DomainErrorKind.Conflict, $"Screening {screeningId} started at {startedAt:u} and can no longer change.")
    {
        AddExtension("screeningId", screeningId);
        AddExtension("startedAt", startedAt);
    }
}
