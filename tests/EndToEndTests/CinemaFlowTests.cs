using System.Net;
using System.Net.Http.Json;
using QubicaCinema.EndToEndTests.Fixtures;

namespace QubicaCinema.EndToEndTests;

/// <summary>
/// The flow the whole solution exists to support, walked once through the real topology: log in as the
/// seeded administrator, schedule a screening, wait for Booking to see it, register a customer, book a
/// seat, watch a second customer collide with the same seat, cancel, and see the seat come back.
/// </summary>
public sealed class CinemaFlowTests(CinemaAppFixture cinema)
{
    [Fact]
    public async Task Booking_a_seat_end_to_end_through_the_gateway()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // The administrator src/MigrationService seeded, from src/AppHost/appsettings.json.
        using HttpClient admin = await cinema.Gateway.LoginAsAsync(
            "admin@qubicacinema.local", "Admin!Cinema1", cancellationToken);

        Guid auditoriumId = await admin.CreateAuditoriumAsync($"E2E room {Guid.NewGuid():N}", cancellationToken: cancellationToken);
        Guid movieId = await admin.CreateMovieAsync($"E2E film {Guid.NewGuid():N}", cancellationToken: cancellationToken);

        using HttpResponseMessage schedule = await admin.ScheduleScreeningAsync(
            movieId, auditoriumId, DateTimeOffset.UtcNow.AddDays(1), cancellationToken: cancellationToken);
        schedule.StatusCode.ShouldBe(HttpStatusCode.Created);
        var screeningId = Guid.Parse(schedule.Headers.Location!.OriginalString.Split('/')[^1]);

        // Proves the event actually crossed the real broker: there is no outbox to pump by hand here.
        SeatMapResponse seatMap = await cinema.Gateway.WaitForSeatMapAsync(screeningId, TimeSpan.FromSeconds(30), cancellationToken);
        Guid seatId = seatMap.Seats[0].SeatId;

        using HttpClient firstCustomer = await cinema.Gateway.RegisterCustomerAsync(cancellationToken);
        using HttpResponseMessage firstBooking = await firstCustomer.BookSeatsAsync(
            screeningId, [seatId], Guid.NewGuid().ToString(), cancellationToken);
        firstBooking.StatusCode.ShouldBe(HttpStatusCode.Created);
        BookingResponse booking = (await firstBooking.Content.ReadFromJsonAsync<BookingResponse>(TestJson.Options, cancellationToken))!;

        using HttpClient secondCustomer = await cinema.Gateway.RegisterCustomerAsync(cancellationToken);
        using HttpResponseMessage secondBooking = await secondCustomer.BookSeatsAsync(
            screeningId, [seatId], Guid.NewGuid().ToString(), cancellationToken);
        secondBooking.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using HttpResponseMessage cancellation =
            await firstCustomer.PostAsync($"/api/v1/bookings/{booking.Id}/cancellation", null, cancellationToken);
        cancellation.StatusCode.ShouldBe(HttpStatusCode.OK);

        SeatMapResponse afterwards = (await cinema.Gateway.GetFromJsonAsync<SeatMapResponse>(
            $"/api/v1/screenings/{screeningId}/seats", TestJson.Options, cancellationToken))!;
        afterwards.Seats.Single(seat => seat.SeatId == seatId).IsAvailable.ShouldBeTrue();
    }
}
