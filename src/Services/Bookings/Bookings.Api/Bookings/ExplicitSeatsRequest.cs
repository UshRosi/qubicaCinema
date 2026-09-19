using QubicaCinema.Bookings.Domain.SeatAllocation;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>Book these exact seats: <c>{"mode":"seats","seatIds":["…"]}</c>.</summary>
/// <param name="SeatIds">The seats, by id, as the seat map lists them.</param>
public sealed record ExplicitSeatsRequest(IReadOnlyList<Guid> SeatIds) : SeatSelectionRequest
{
    /// <inheritdoc />
    internal override SeatSelection ToSelection() => new SeatSelection.Explicit(SeatIds);
}
