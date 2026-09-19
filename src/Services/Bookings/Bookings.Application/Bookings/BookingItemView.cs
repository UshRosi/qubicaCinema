using QubicaCinema.Bookings.Domain.Bookings;

namespace QubicaCinema.Bookings.Application.Bookings;

/// <summary>One booked seat, as a ticket shows it.</summary>
/// <param name="Id">The item's id, for cancelling it on its own.</param>
/// <param name="ScreeningId">The screening.</param>
/// <param name="MovieTitle">What is being shown.</param>
/// <param name="AuditoriumName">Where.</param>
/// <param name="StartsAt">When the audience is admitted.</param>
/// <param name="SeatId">The seat.</param>
/// <param name="Row">The seat's row.</param>
/// <param name="Number">The seat's number within the row.</param>
/// <param name="PriceAmount">What the seat cost when it was booked.</param>
/// <param name="PriceCurrency">The currency of that price.</param>
/// <param name="Status">Whether the seat is still held.</param>
public sealed record BookingItemView(
    Guid Id,
    Guid ScreeningId,
    string MovieTitle,
    string AuditoriumName,
    DateTimeOffset StartsAt,
    Guid SeatId,
    string Row,
    int Number,
    decimal PriceAmount,
    string PriceCurrency,
    BookingItemStatus Status);
