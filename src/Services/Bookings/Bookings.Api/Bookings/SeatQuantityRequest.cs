using QubicaCinema.Bookings.Domain.SeatAllocation;
using QubicaCinema.Bookings.Domain.ValueObjects;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>Book this many seats, side by side, wherever they are best: <c>{"mode":"quantity","quantity":2}</c>.</summary>
/// <param name="Quantity">How many seats.</param>
public sealed record SeatQuantityRequest(int Quantity) : SeatSelectionRequest
{
    /// <inheritdoc />
    internal override SeatSelection ToSelection() => new SeatSelection.ByQuantity(SeatCount.Of(Quantity));
}
