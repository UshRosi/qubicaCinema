using System.Net;
using System.Text.Json;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Catalog;

/// <summary>
/// Proves that the cancellation body is optional, as its only field is, and that a body which is sent is
/// still validated.
/// </summary>
public sealed class ScreeningCancellationTests(CinemaFixture cinema)
{
    [Fact]
    public async Task A_screening_can_be_cancelled_without_a_body()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient admin = cinema.Catalog.CreateClientFor(Guid.CreateVersion7(), CinemaRoles.Admin);
        Guid screeningId = await ScheduleAsync(admin, cancellationToken);

        using HttpResponseMessage response =
            await admin.PostAsync($"/api/v1/screenings/{screeningId}/cancellation", content: null, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement screening = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/screenings/{screeningId}", TestJson.Options, cancellationToken);
        screening.GetProperty("status").GetString().ShouldBe("Cancelled");
    }

    [Fact]
    public async Task A_cancellation_reason_that_is_too_long_is_answered_422()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient admin = cinema.Catalog.CreateClientFor(Guid.CreateVersion7(), CinemaRoles.Admin);
        Guid screeningId = await ScheduleAsync(admin, cancellationToken);

        using HttpResponseMessage response = await admin.PostAsJsonAsync(
            $"/api/v1/screenings/{screeningId}/cancellation", new { reason = new string('x', 501) }, cancellationToken);

        response.StatusCode.ShouldBe((HttpStatusCode)422);
    }

    private async Task<Guid> ScheduleAsync(HttpClient admin, CancellationToken cancellationToken)
    {
        Guid auditoriumId = await admin.CreateAuditoriumAsync($"Room {Guid.NewGuid():N}", cancellationToken: cancellationToken);
        Guid movieId = await admin.CreateMovieAsync($"Film {Guid.NewGuid():N}", cancellationToken: cancellationToken);

        using HttpResponseMessage schedule = await admin.ScheduleScreeningAsync(
            movieId, auditoriumId, cinema.Clock.GetUtcNow().AddHours(6), cancellationToken: cancellationToken);
        schedule.EnsureSuccessStatusCode();

        return Guid.Parse(schedule.Headers.Location!.OriginalString.Split('/')[^1]);
    }
}
