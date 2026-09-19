using QubicaCinema.Bookings.Domain.SeatAllocation;

namespace QubicaCinema.Bookings.Application.Bookings.CreateBooking;

/// <summary>Books seats at one or more screenings, for the current user, in one purchase.</summary>
/// <param name="Items">The seats wanted at each screening.</param>
public sealed record CreateBookingCommand(IReadOnlyList<ScreeningSelection> Items)
{
    /// <summary>
    /// Whether the seats at this screening were left to the allocation strategy. Only those can be chosen
    /// again after losing a race: seats the customer named are the seats they want, and nothing else will do.
    /// </summary>
    internal bool AllocatesAutomatically(Guid screeningId) =>
        Items.Any(item => item.ScreeningId == screeningId && item.Selection is SeatSelection.ByQuantity);
}
