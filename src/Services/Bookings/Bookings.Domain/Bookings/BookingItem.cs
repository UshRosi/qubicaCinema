using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Bookings;

/// <summary>
/// One seat at one screening, held by a booking.
/// </summary>
/// <remarks>
/// Part of the <see cref="Booking"/> aggregate: only the booking creates or cancels an item, which is why
/// the constructor and <see cref="Cancel"/> are internal. The price is a snapshot taken when the seat was
/// booked, so a later price change in Catalog never rewrites what a customer already paid.
/// </remarks>
public sealed class BookingItem : Entity<Guid>
{
    internal BookingItem(Guid id, Guid screeningId, Guid seatId, Money price)
        : base(id)
    {
        ScreeningId = screeningId;
        SeatId = seatId;
        Price = price;
        Status = BookingItemStatus.Active;
    }

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private BookingItem() => Price = null!;

    /// <summary>The screening the seat is booked for.</summary>
    public Guid ScreeningId { get; private set; }

    /// <summary>The seat.</summary>
    public Guid SeatId { get; private set; }

    /// <summary>What the seat cost when it was booked.</summary>
    public Money Price { get; private set; }

    /// <summary>Whether the seat is still held.</summary>
    public BookingItemStatus Status { get; private set; }

    /// <summary>When the seat was given back, or null while it is still held.</summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>Whether the seat is still held.</summary>
    public bool IsActive => Status == BookingItemStatus.Active;

    /// <summary>Gives the seat back. The booking decides whether that is allowed.</summary>
    internal void Cancel(DateTimeOffset now)
    {
        Status = BookingItemStatus.Cancelled;
        CancelledAt = now;
    }
}
