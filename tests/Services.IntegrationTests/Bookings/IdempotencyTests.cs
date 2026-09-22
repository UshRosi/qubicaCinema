using System.Net;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Bookings;

/// <summary>
/// Proves that the <c>Idempotency-Key</c> record, written in the same transaction as the booking, actually
/// stops a retried request from booking twice.
/// </summary>
public sealed class IdempotencyTests(CinemaFixture cinema)
{
    [Fact]
    public async Task The_same_key_replays_the_first_response_instead_of_booking_again()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (Guid screeningId, SeatMapResponse seatMap) = await cinema.ScheduleAndProjectAsync(cancellationToken);
        Guid seatId = seatMap.Seats[0].SeatId;
        string key = Guid.NewGuid().ToString();

        HttpClient customer = cinema.NewCustomer();

        using HttpResponseMessage first = await customer.BookSeatsAsync(screeningId, [seatId], key, cancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        BookingResponse firstBody = (await first.Content.ReadFromJsonAsync<BookingResponse>(TestJson.Options, cancellationToken))!;

        using HttpResponseMessage replay = await customer.BookSeatsAsync(screeningId, [seatId], key, cancellationToken);

        replay.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.Headers.GetValues("Idempotency-Replayed").ShouldContain("true");
        BookingResponse replayBody = (await replay.Content.ReadFromJsonAsync<BookingResponse>(TestJson.Options, cancellationToken))!;
        replayBody.Id.ShouldBe(firstBody.Id);

        SeatMapResponse afterwards = (await customer.GetFromJsonAsync<SeatMapResponse>(
            $"/api/v1/screenings/{screeningId}/seats", TestJson.Options, cancellationToken))!;
        // Exactly one seat taken: a replay that had booked again would take two.
        (seatMap.Counts.Available - afterwards.Counts.Available).ShouldBe(1);
    }

    [Fact]
    public async Task The_same_key_with_a_different_body_is_rejected()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (Guid screeningId, SeatMapResponse seatMap) = await cinema.ScheduleAndProjectAsync(cancellationToken);
        string key = Guid.NewGuid().ToString();

        HttpClient customer = cinema.NewCustomer();

        using HttpResponseMessage first =
            await customer.BookSeatsAsync(screeningId, [seatMap.Seats[0].SeatId], key, cancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        using HttpResponseMessage second =
            await customer.BookSeatsAsync(screeningId, [seatMap.Seats[1].SeatId], key, cancellationToken);

        second.StatusCode.ShouldBe((HttpStatusCode)422);
    }
}
