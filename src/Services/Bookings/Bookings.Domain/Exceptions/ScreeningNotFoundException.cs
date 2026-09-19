using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>No screening with this id is known to the Booking service.</summary>
public sealed class ScreeningNotFoundException : DomainException
{
    /// <summary>Creates the exception from the id that was looked up.</summary>
    public ScreeningNotFoundException(Guid screeningId)
        : base(DomainErrorKind.NotFound, $"No screening with id {screeningId} exists.") =>
        AddExtension("screeningId", screeningId);
}
