using System.Net;
using System.Text.Json;
using QubicaCinema.EndToEndTests.Fixtures;

namespace QubicaCinema.EndToEndTests;

/// <summary>
/// The two things only the full topology can prove: a token minted by the real Identity service is accepted
/// by another service, and <c>/api/v1/screenings/**</c> is genuinely split between Catalog and Booking by
/// the gateway's route order.
/// </summary>
public sealed class GatewayInteropTests(CinemaAppFixture cinema)
{
    [Fact]
    public async Task A_token_Identity_issued_is_accepted_by_Booking()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Registration and login both go through the gateway to Identity; the request below goes through
        // the gateway to Booking. Nothing here shares a process, a signing key value in code, or a database —
        // only the Jwt:SigningKey the AppHost fans out to both from one parameter.
        using HttpClient customer = await cinema.Gateway.RegisterCustomerAsync(cancellationToken);

        using HttpResponseMessage response = await customer.GetAsync("/api/v1/bookings", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_seat_map_reaches_Booking_while_the_screening_itself_reaches_Catalog()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using HttpClient admin = await cinema.Gateway.LoginAsAsync("admin@qubicacinema.local", "Admin!Cinema1", cancellationToken);
        Guid auditoriumId = await admin.CreateAuditoriumAsync($"Interop room {Guid.NewGuid():N}", cancellationToken: cancellationToken);
        Guid movieId = await admin.CreateMovieAsync($"Interop film {Guid.NewGuid():N}", cancellationToken: cancellationToken);

        using HttpResponseMessage schedule = await admin.ScheduleScreeningAsync(
            movieId, auditoriumId, DateTimeOffset.UtcNow.AddDays(2), cancellationToken: cancellationToken);
        var screeningId = Guid.Parse(schedule.Headers.Location!.OriginalString.Split('/')[^1]);

        // Proves the seat map reaches Booking before asserting on it, without touching Catalog's own answer.
        await cinema.Gateway.WaitForSeatMapAsync(screeningId, TimeSpan.FromSeconds(30), cancellationToken);

        using HttpResponseMessage screening = await cinema.Gateway.GetAsync($"/api/v1/screenings/{screeningId}", cancellationToken);
        using HttpResponseMessage seats = await cinema.Gateway.GetAsync($"/api/v1/screenings/{screeningId}/seats", cancellationToken);

        // Catalog's ScreeningView carries movieTitle and no seat map; Booking's SeatAvailabilityView carries
        // counts and a list of seats. Neither service could have answered the other's request.
        JsonElement screeningBody = JsonDocument.Parse(await screening.Content.ReadAsStringAsync(cancellationToken)).RootElement;
        JsonElement seatsBody = JsonDocument.Parse(await seats.Content.ReadAsStringAsync(cancellationToken)).RootElement;

        screeningBody.TryGetProperty("movieTitle", out _).ShouldBeTrue();
        screeningBody.TryGetProperty("counts", out _).ShouldBeFalse();

        seatsBody.TryGetProperty("counts", out _).ShouldBeTrue();
        seatsBody.TryGetProperty("seats", out _).ShouldBeTrue();
    }
}
