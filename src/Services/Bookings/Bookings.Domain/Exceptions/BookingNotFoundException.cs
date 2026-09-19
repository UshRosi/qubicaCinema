using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>
/// No booking with this id exists — or none that the caller is allowed to know about.
/// </summary>
/// <remarks>
/// The two cases are one exception on purpose. Answering 403 for someone else's booking would confirm that
/// the id exists, and booking ids would become something to enumerate. A caller who does not own a booking
/// learns exactly as much as a caller asking for an id that was never issued.
/// </remarks>
public sealed class BookingNotFoundException : DomainException
{
    /// <summary>Creates the exception from the id that was looked up.</summary>
    public BookingNotFoundException(Guid bookingId)
        : base(DomainErrorKind.NotFound, $"No booking with id {bookingId} exists.") =>
        AddExtension("bookingId", bookingId);
}
