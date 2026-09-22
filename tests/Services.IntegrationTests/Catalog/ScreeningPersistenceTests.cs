using System.Net;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Catalog;

/// <summary>
/// Proves the two guarantees that only a real SQL Server enforces: the filtered index behind
/// <c>AuditoriumNameAlreadyUsedException</c>, and the <c>RowVersion</c> behind a stale <c>If-Match</c>.
/// </summary>
public sealed class ScreeningPersistenceTests(CinemaFixture cinema)
{
    [Fact]
    public async Task Should_reject_a_second_auditorium_with_the_same_name()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient admin = cinema.Catalog.CreateClientFor(Guid.CreateVersion7(), CinemaRoles.Admin);
        string name = $"Duplicate room {Guid.NewGuid():N}";

        (await admin.CreateAuditoriumAsync(name, cancellationToken: cancellationToken)).ShouldNotBe(Guid.Empty);

        using HttpResponseMessage second = await admin.PostAsJsonAsync(
            "/api/v1/auditoriums", new { name, rowCount = 2, seatsPerRow = 2 }, cancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Should_reject_rescheduling_with_a_stale_If_Match()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient admin = cinema.Catalog.CreateClientFor(Guid.CreateVersion7(), CinemaRoles.Admin);

        Guid auditoriumId = await admin.CreateAuditoriumAsync(
            $"RowVersion room {Guid.NewGuid():N}", cancellationToken: cancellationToken);
        Guid movieId = await admin.CreateMovieAsync($"RowVersion film {Guid.NewGuid():N}", cancellationToken: cancellationToken);
        DateTimeOffset startsAt = cinema.Clock.GetUtcNow().AddHours(5);

        using HttpResponseMessage schedule = await admin.ScheduleScreeningAsync(
            movieId, auditoriumId, startsAt, cancellationToken: cancellationToken);
        var screeningId = Guid.Parse(schedule.Headers.Location!.OriginalString.Split('/')[^1]);

        string staleETag = await GetETagAsync(admin, screeningId, cancellationToken);

        // Moves the screening on with the correct tag, which changes its RowVersion and makes staleETag stale.
        using HttpRequestMessage firstReschedule = new(HttpMethod.Put, $"/api/v1/screenings/{screeningId}")
        {
            Content = JsonContent.Create(new { startsAt = startsAt.AddHours(1), priceAmount = 9.5m, priceCurrency = "EUR" }),
        };
        firstReschedule.Headers.Add("If-Match", staleETag);
        using HttpResponseMessage firstResponse = await admin.SendAsync(firstReschedule, cancellationToken);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using HttpRequestMessage secondReschedule = new(HttpMethod.Put, $"/api/v1/screenings/{screeningId}")
        {
            Content = JsonContent.Create(new { startsAt = startsAt.AddHours(2), priceAmount = 9.5m, priceCurrency = "EUR" }),
        };
        secondReschedule.Headers.Add("If-Match", staleETag);
        using HttpResponseMessage secondResponse = await admin.SendAsync(secondReschedule, cancellationToken);

        secondResponse.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    private static async Task<string> GetETagAsync(HttpClient admin, Guid screeningId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await admin.GetAsync($"/api/v1/screenings/{screeningId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return response.Headers.GetValues("ETag").Single();
    }
}
