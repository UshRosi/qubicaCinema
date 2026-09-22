using System.Net;
using System.Net.Http.Json;

namespace QubicaCinema.EndToEndTests.Fixtures;

/// <summary>The JSON shapes Booking answers with, read as plain data — this project references no Api assembly at all.</summary>
internal sealed record SeatMapResponse(Guid ScreeningId, string Status, SeatCountsResponse Counts, IReadOnlyList<SeatStateResponse> Seats);

internal sealed record SeatCountsResponse(int Total, int Available, int Booked);

internal sealed record SeatStateResponse(Guid SeatId, string Row, int Number, bool IsAvailable);

internal sealed record BookingResponse(Guid Id, Guid UserId, string Status, IReadOnlyList<BookingItemResponse> Items);

internal sealed record BookingItemResponse(Guid Id, Guid ScreeningId, Guid SeatId, string Status);

/// <summary>Reads Booking's seat map, and books seats, through the gateway.</summary>
internal static class BookingFlows
{
    /// <summary>
    /// Waits until Booking's own seat map for the screening exists — proof that Catalog's
    /// <c>ScreeningScheduled</c> event crossed the real broker and was applied.
    /// </summary>
    internal static async Task<SeatMapResponse> WaitForSeatMapAsync(
        this HttpClient gateway, Guid screeningId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        SeatMapResponse? seatMap = null;

        await Eventually.UntilAsync(
            async () =>
            {
                using HttpResponseMessage response =
                    await gateway.GetAsync($"/api/v1/screenings/{screeningId}/seats", cancellationToken);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                seatMap = await response.Content.ReadFromJsonAsync<SeatMapResponse>(TestJson.Options, cancellationToken);

                return seatMap is not null;
            },
            because: $"Booking to have projected screening {screeningId}",
            timeout);

        return seatMap!;
    }

    /// <summary>Books the given exact seats at one screening.</summary>
    internal static async Task<HttpResponseMessage> BookSeatsAsync(
        this HttpClient customer,
        Guid screeningId,
        IReadOnlyList<Guid> seatIds,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/bookings")
        {
            Content = JsonContent.Create(new { items = new[] { new { screeningId, selection = new { mode = "seats", seatIds } } } }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        return await customer.SendAsync(request, cancellationToken);
    }
}
