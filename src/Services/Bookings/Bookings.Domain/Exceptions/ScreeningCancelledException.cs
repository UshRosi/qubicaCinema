using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>The screening was called off, so no seat at it can be booked.</summary>
public sealed class ScreeningCancelledException : DomainException
{
    /// <summary>Creates the exception from the screening that was cancelled.</summary>
    public ScreeningCancelledException(Guid screeningId)
        : base(DomainErrorKind.Conflict, $"Screening {screeningId} was cancelled; its seats can no longer be booked.") =>
        AddExtension("screeningId", screeningId);
}
