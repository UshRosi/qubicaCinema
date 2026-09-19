using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>The booking has no item with this id.</summary>
public sealed class BookingItemNotFoundException : DomainException
{
    /// <summary>Creates the exception from the booking and the item id that was looked up.</summary>
    public BookingItemNotFoundException(Guid bookingId, Guid itemId)
        : base(DomainErrorKind.NotFound, $"Booking {bookingId} has no item with id {itemId}.")
    {
        AddExtension("bookingId", bookingId);
        AddExtension("itemId", itemId);
    }
}
