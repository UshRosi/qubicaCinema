using System.Net;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Bookings;

/// <summary>Proves that booking by quantity, through <c>CenterFirstAdjacentSeatsStrategy</c>, really seats people together.</summary>
public sealed class SeatAllocationTests(CinemaFixture cinema)
{
    [Fact]
    public async Task Booking_by_quantity_allocates_adjacent_seats_in_one_row_and_moves_the_counts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (Guid screeningId, SeatMapResponse before) = await cinema.ScheduleAndProjectAsync(cancellationToken);

        HttpClient customer = cinema.NewCustomer();
        using HttpResponseMessage response =
            await customer.BookQuantityAsync(screeningId, quantity: 3, Guid.NewGuid().ToString(), cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        BookingResponse booking = (await response.Content.ReadFromJsonAsync<BookingResponse>(TestJson.Options, cancellationToken))!;
        booking.Items.Count.ShouldBe(3);

        HashSet<Guid> bookedSeatIds = [.. booking.Items.Select(item => item.SeatId)];
        List<SeatStateResponse> bookedSeats =
            [.. before.Seats.Where(seat => bookedSeatIds.Contains(seat.SeatId)).OrderBy(seat => seat.Number)];

        bookedSeats.Select(seat => seat.Row).Distinct().ShouldHaveSingleItem();
        for (int index = 1; index < bookedSeats.Count; index++)
        {
            bookedSeats[index].Number.ShouldBe(bookedSeats[index - 1].Number + 1);
        }

        SeatMapResponse after = (await customer.GetFromJsonAsync<SeatMapResponse>(
            $"/api/v1/screenings/{screeningId}/seats", TestJson.Options, cancellationToken))!;
        after.Counts.Available.ShouldBe(before.Counts.Available - 3);
        after.Counts.Booked.ShouldBe(before.Counts.Booked + 3);
    }
}
