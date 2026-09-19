using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>A seat in the booking is for a screening that has already started, so it can no longer be given back.</summary>
public sealed class BookingNotCancellableException : DomainException
{
    /// <summary>Creates the exception from the booking and the screening that has started.</summary>
    public BookingNotCancellableException(Guid bookingId, Guid screeningId, DateTimeOffset startsAt)
        : base(
            DomainErrorKind.Conflict,
            $"Booking {bookingId} includes screening {screeningId}, which started at {startsAt:u}; it can no longer be cancelled.")
    {
        AddExtension("bookingId", bookingId);
        AddExtension("screeningId", screeningId);
        AddExtension("startsAt", startsAt);
    }
}
