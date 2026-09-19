using QubicaCinema.Bookings.Application.Bookings.CreateBooking;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>
/// The body of <c>POST /bookings</c>: seats at one or more screenings, bought together.
/// </summary>
/// <param name="Items">One entry per screening.</param>
public sealed record CreateBookingRequest(IReadOnlyList<BookingItemRequest> Items)
{
    /// <summary>Converts the validated request into the command the use case understands.</summary>
    internal CreateBookingCommand ToCommand() =>
        new([.. Items.Select(item => new ScreeningSelection(item.ScreeningId, item.Selection.ToSelection()))]);
}
