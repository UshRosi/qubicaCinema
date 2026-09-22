using System.Net;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>The JSON shapes Booking answers with, read as plain data rather than through the aliased Api assembly.</summary>
internal sealed record SeatMapResponse(Guid ScreeningId, string Status, SeatCountsResponse Counts, IReadOnlyList<SeatStateResponse> Seats);

internal sealed record SeatCountsResponse(int Total, int Available, int Booked);

internal sealed record SeatStateResponse(Guid SeatId, string Row, int Number, bool IsAvailable);

internal sealed record BookingResponse(Guid Id, Guid UserId, string Status, IReadOnlyList<BookingItemResponse> Items);

internal sealed record BookingItemResponse(Guid Id, Guid ScreeningId, Guid SeatId, string Status);

/// <summary>Reads Booking's projection of a screening, and books seats on it, through the real endpoints.</summary>
internal static class BookingsFlows
{
    /// <summary>
    /// Waits until Booking's own seat map for the screening exists, which is only true once Catalog's
    /// <c>ScreeningScheduled</c> event has crossed RabbitMQ and been applied by <c>ScreeningScheduledHandler</c>.
    /// </summary>
    internal static async Task<SeatMapResponse> WaitForSeatMapAsync(
        this HttpClient client, Guid screeningId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        SeatMapResponse? seatMap = null;

        await Eventually.UntilAsync(
            async () =>
            {
                using HttpResponseMessage response =
                    await client.GetAsync($"/api/v1/screenings/{screeningId}/seats", cancellationToken);

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

    /// <summary>Waits until the given seat is available again, for example after a screening was cancelled.</summary>
    internal static async Task<SeatMapResponse> WaitForReleasedSeatAsync(
        this HttpClient client, Guid screeningId, Guid seatId, CancellationToken cancellationToken)
    {
        SeatMapResponse? seatMap = null;

        await Eventually.UntilAsync(
            async () =>
            {
                seatMap = await client.GetFromJsonAsync<SeatMapResponse>(
                    $"/api/v1/screenings/{screeningId}/seats", TestJson.Options, cancellationToken);

                return seatMap!.Seats.Single(seat => seat.SeatId == seatId).IsAvailable;
            },
            because: $"seat {seatId} of screening {screeningId} to be released",
            TimeSpan.FromSeconds(10));

        return seatMap!;
    }

    /// <summary>Books the given exact seats at one screening.</summary>
    internal static Task<HttpResponseMessage> BookSeatsAsync(
        this HttpClient customer,
        Guid screeningId,
        IReadOnlyList<Guid> seatIds,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        SendBookingAsync(
            customer,
            new { items = new[] { new { screeningId, selection = new { mode = "seats", seatIds } } } },
            idempotencyKey,
            cancellationToken);

    /// <summary>Books a number of seats at one screening, letting the allocation strategy choose them.</summary>
    internal static Task<HttpResponseMessage> BookQuantityAsync(
        this HttpClient customer,
        Guid screeningId,
        int quantity,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        SendBookingAsync(
            customer,
            new { items = new[] { new { screeningId, selection = new { mode = "quantity", quantity } } } },
            idempotencyKey,
            cancellationToken);

    private static async Task<HttpResponseMessage> SendBookingAsync(
        HttpClient customer, object body, string idempotencyKey, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/bookings")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        return await customer.SendAsync(request, cancellationToken);
    }
}
