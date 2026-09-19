using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>The screening has already begun, so its seats can no longer be booked.</summary>
public sealed class ScreeningStartedException : DomainException
{
    /// <summary>Creates the exception from the screening and the time it started.</summary>
    public ScreeningStartedException(Guid screeningId, DateTimeOffset startsAt)
        : base(DomainErrorKind.Conflict, $"Screening {screeningId} started at {startsAt:u}; its seats can no longer be booked.")
    {
        AddExtension("screeningId", screeningId);
        AddExtension("startsAt", startsAt);
    }
}
