using System.Net;
using System.Text.Json.Nodes;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Bookings;

/// <summary>
/// The headline of this chapter: the filtered unique index on <c>BookingItems(ScreeningId, SeatId)</c>, the
/// only thing standing between two customers and the same seat.
/// </summary>
public sealed class DoubleBookingTests(CinemaFixture cinema)
{
    [Fact]
    public async Task Two_customers_booking_the_same_seat_get_exactly_one_201_and_one_409()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (Guid screeningId, SeatMapResponse seatMap) =
            await cinema.ScheduleAndProjectAsync(cancellationToken);
        Guid seatId = seatMap.Seats[0].SeatId;

        HttpClient first = cinema.NewCustomer();
        HttpClient second = cinema.NewCustomer();

        HttpResponseMessage[] responses = await Task.WhenAll(
            first.BookSeatsAsync(screeningId, [seatId], Guid.NewGuid().ToString(), cancellationToken),
            second.BookSeatsAsync(screeningId, [seatId], Guid.NewGuid().ToString(), cancellationToken));

        try
        {
            responses.Count(response => response.StatusCode == HttpStatusCode.Created).ShouldBe(1);
            responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);

            HttpResponseMessage conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
            JsonNode problem = JsonNode.Parse(await conflict.Content.ReadAsStringAsync(cancellationToken))!;
            problem["unavailableSeatIds"]!.AsArray().Select(id => Guid.Parse(id!.GetValue<string>()))
                .ShouldContain(seatId);
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task Cancelling_a_booking_frees_the_seat_for_a_new_one()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (Guid screeningId, SeatMapResponse seatMap) =
            await cinema.ScheduleAndProjectAsync(cancellationToken);
        Guid seatId = seatMap.Seats[0].SeatId;

        HttpClient firstCustomer = cinema.NewCustomer();
        using HttpResponseMessage firstBooking = await firstCustomer.BookSeatsAsync(
            screeningId, [seatId], Guid.NewGuid().ToString(), cancellationToken);
        firstBooking.StatusCode.ShouldBe(HttpStatusCode.Created);
        BookingResponse booking = (await firstBooking.Content.ReadFromJsonAsync<BookingResponse>(
            TestJson.Options, cancellationToken))!;

        using HttpResponseMessage cancellation =
            await firstCustomer.PostAsync($"/api/v1/bookings/{booking.Id}/cancellation", null, cancellationToken);
        cancellation.StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpClient secondCustomer = cinema.NewCustomer();
        using HttpResponseMessage secondBooking = await secondCustomer.BookSeatsAsync(
            screeningId, [seatId], Guid.NewGuid().ToString(), cancellationToken);

        secondBooking.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
