using QubicaCinema.BuildingBlocks.Application.Security;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>The setup every Bookings test starts from: a real screening, announced and projected.</summary>
internal static class BookingScenario
{
    /// <summary>
    /// Puts a film on the programme through Catalog, drains its outbox, and waits until Booking has built
    /// its own seat map for it — the same three steps every Bookings test needs before it can book anything.
    /// </summary>
    internal static async Task<(Guid ScreeningId, SeatMapResponse SeatMap)> ScheduleAndProjectAsync(
        this CinemaFixture cinema, CancellationToken cancellationToken)
    {
        HttpClient admin = cinema.Catalog.CreateClientFor(Guid.CreateVersion7(), CinemaRoles.Admin);

        // A short, merely-unique name: Auditorium.MaxNameLength is 100, and a descriptive test method name
        // plus a GUID would overrun it.
        Guid auditoriumId = await admin.CreateAuditoriumAsync($"Room {Guid.NewGuid():N}", cancellationToken: cancellationToken);
        Guid movieId = await admin.CreateMovieAsync($"Film {Guid.NewGuid():N}", cancellationToken: cancellationToken);

        using HttpResponseMessage schedule = await admin.ScheduleScreeningAsync(
            movieId, auditoriumId, cinema.Clock.GetUtcNow().AddHours(6), cancellationToken: cancellationToken);
        schedule.EnsureSuccessStatusCode();
        var screeningId = Guid.Parse(schedule.Headers.Location!.OriginalString.Split('/')[^1]);

        await Outbox.DrainAsync(cinema.Catalog, cancellationToken);

        HttpClient anonymous = cinema.Bookings.CreateClient();
        SeatMapResponse seatMap =
            await anonymous.WaitForSeatMapAsync(screeningId, TimeSpan.FromSeconds(15), cancellationToken);

        return (screeningId, seatMap);
    }

    /// <summary>A client presenting a valid token for a fresh customer.</summary>
    internal static HttpClient NewCustomer(this CinemaFixture cinema) =>
        cinema.Bookings.CreateClientFor(Guid.CreateVersion7(), CinemaRoles.Customer);
}
