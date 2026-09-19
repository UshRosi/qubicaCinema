using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.Screenings;

/// <summary>
/// The seat map of a screening: which seats exist and which can be booked.
/// </summary>
/// <remarks>
/// A snapshot, not a hold. A seat shown free here can be taken a moment later; the authoritative answer is
/// the 409 at booking time. The seats are a flat list rather than a dictionary keyed by row letter: a list
/// of objects is easy to version and to describe in OpenAPI, and a client groups it however it draws it.
/// </remarks>
/// <param name="ScreeningId">The screening.</param>
/// <param name="MovieTitle">What is being shown.</param>
/// <param name="AuditoriumId">Where.</param>
/// <param name="AuditoriumName">The auditorium's name.</param>
/// <param name="StartsAt">When the audience is admitted.</param>
/// <param name="PriceAmount">What one seat costs.</param>
/// <param name="PriceCurrency">The currency of that price.</param>
/// <param name="Status">Whether the screening is still going to happen.</param>
/// <param name="Counts">How many seats there are, and how many are free.</param>
/// <param name="Seats">Every seat, ordered by row and number.</param>
public sealed record SeatAvailabilityView(
    Guid ScreeningId,
    string MovieTitle,
    Guid AuditoriumId,
    string AuditoriumName,
    DateTimeOffset StartsAt,
    decimal PriceAmount,
    string PriceCurrency,
    ScreeningStatus Status,
    SeatCountsView Counts,
    IReadOnlyList<SeatStateView> Seats)
{
    /// <summary>Describes a seat map. Availability is the map's own answer, so it cannot differ from what booking sees.</summary>
    public static SeatAvailabilityView From(SeatMap seatMap)
    {
        Screening screening = seatMap.Screening;

        return new SeatAvailabilityView(
            screening.Id,
            screening.MovieTitle,
            screening.AuditoriumId,
            screening.AuditoriumName,
            screening.StartsAt,
            screening.Price.Amount,
            screening.Price.Currency,
            screening.Status,
            new SeatCountsView(seatMap.Seats.Count, seatMap.AvailableCount, seatMap.BookedCount),
            [.. seatMap.Seats.Select(seat =>
                new SeatStateView(seat.Id, seat.Position.Row, seat.Position.Number, seatMap.IsAvailable(seat)))]);
    }
}
