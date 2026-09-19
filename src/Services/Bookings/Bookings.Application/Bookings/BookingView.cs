using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.Bookings;

/// <summary>
/// A booking as its owner sees it: every seat, what each cost, and what is still held.
/// </summary>
/// <param name="Id">The booking's id.</param>
/// <param name="UserId">Who made it.</param>
/// <param name="Status">Whether it still holds any seat.</param>
/// <param name="CreatedAt">When it was made.</param>
/// <param name="CancelledAt">When its last seat was given back, if it was.</param>
/// <param name="TotalAmount">What the seats still held cost.</param>
/// <param name="TotalCurrency">The currency of that total.</param>
/// <param name="Items">Every seat ever booked, cancelled ones included, in screening and seat order.</param>
public sealed record BookingView(
    Guid Id,
    Guid UserId,
    BookingStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CancelledAt,
    decimal TotalAmount,
    string TotalCurrency,
    IReadOnlyList<BookingItemView> Items)
{
    /// <summary>
    /// Describes a booking, taking the screening and seat details from the local read model.
    /// </summary>
    /// <remarks>
    /// The total is read from <see cref="Booking.Total"/>, never summed here: the aggregate is the one place
    /// that decides which items count.
    /// </remarks>
    /// <param name="booking">The booking.</param>
    /// <param name="screenings">Every screening its items refer to, by id.</param>
    /// <param name="seats">Every seat its items refer to, by id.</param>
    public static BookingView From(
        Booking booking,
        IReadOnlyDictionary<Guid, Screening> screenings,
        IReadOnlyDictionary<Guid, Seat> seats)
    {
        BookingItemView[] items = [.. booking.Items
            .Select(item => DescribeItem(item, screenings[item.ScreeningId], seats[item.SeatId]))
            .OrderBy(item => item.StartsAt)
            .ThenBy(item => item.Row, StringComparer.Ordinal)
            .ThenBy(item => item.Number)];

        return new BookingView(
            booking.Id,
            booking.UserId,
            booking.Status,
            booking.CreatedAt,
            booking.CancelledAt,
            booking.Total.Amount,
            booking.Total.Currency,
            items);
    }

    private static BookingItemView DescribeItem(BookingItem item, Screening screening, Seat seat) =>
        new(
            item.Id,
            screening.Id,
            screening.MovieTitle,
            screening.AuditoriumName,
            screening.StartsAt,
            seat.Id,
            seat.Position.Row,
            seat.Position.Number,
            item.Price.Amount,
            item.Price.Currency,
            item.Status);
}
