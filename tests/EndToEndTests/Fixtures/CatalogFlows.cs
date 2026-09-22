using System.Net.Http.Json;

namespace QubicaCinema.EndToEndTests.Fixtures;

/// <summary>Puts a film, a room and a screening on the programme through the gateway's Catalog routes.</summary>
internal static class CatalogFlows
{
    /// <summary>Opens a room with a simple rectangular grid of seats.</summary>
    internal static async Task<Guid> CreateAuditoriumAsync(
        this HttpClient admin, string name, int rowCount = 4, int seatsPerRow = 6, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await admin.PostAsJsonAsync(
            "/api/v1/auditoriums", new { name, rowCount, seatsPerRow }, cancellationToken);

        return IdFromLocation(response);
    }

    /// <summary>Adds a film to the catalogue.</summary>
    internal static async Task<Guid> CreateMovieAsync(
        this HttpClient admin, string title, int durationMinutes = 120, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await admin.PostAsJsonAsync(
            "/api/v1/movies",
            new { title, description = (string?)null, durationMinutes, genre = "Drama", ageRating = "General" },
            cancellationToken);

        return IdFromLocation(response);
    }

    /// <summary>Puts a screening on the programme.</summary>
    internal static Task<HttpResponseMessage> ScheduleScreeningAsync(
        this HttpClient admin,
        Guid movieId,
        Guid auditoriumId,
        DateTimeOffset startsAt,
        decimal priceAmount = 9.5m,
        string priceCurrency = "EUR",
        CancellationToken cancellationToken = default) =>
        admin.PostAsJsonAsync("/api/v1/screenings", new { movieId, auditoriumId, startsAt, priceAmount, priceCurrency }, cancellationToken);

    private static Guid IdFromLocation(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        string location = response.Headers.Location?.OriginalString
            ?? throw new InvalidOperationException("The response carried no Location header.");

        return Guid.Parse(location.Split('/')[^1]);
    }
}
