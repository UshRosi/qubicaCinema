using Microsoft.Net.Http.Headers;
using QubicaCinema.BuildingBlocks.Api.Endpoints;
using QubicaCinema.Bookings.Application.Screenings;
using QubicaCinema.Bookings.Application.Screenings.GetSeatAvailability;

namespace QubicaCinema.Bookings.Api.Screenings;

/// <summary>The seat map of a screening: <c>/api/v1/screenings/{id}/seats</c>.</summary>
/// <remarks>
/// The screening itself is Catalog's resource; only its seats, and whether they are free, are Booking's.
/// The gateway sends this one path here and every other <c>/screenings</c> path to Catalog.
/// </remarks>
internal sealed class SeatMapEndpoints : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/screenings/{id:guid}/seats", GetAsync)
            .WithTags("Seat maps")
            .WithSummary("Returns every seat of a screening and whether it can be booked.")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<SeatAvailabilityView>> GetAsync(
        Guid id,
        GetSeatAvailabilityHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        SeatAvailabilityView seatMap = await handler.HandleAsync(new GetSeatAvailabilityQuery(id), cancellationToken);

        // A snapshot, not a hold: a cached copy would show seats as free after they were taken. Nothing
        // between here and the client may keep it, and the booking's 409 remains the real answer.
        response.Headers[HeaderNames.CacheControl] = "no-store";

        return TypedResults.Ok(seatMap);
    }
}
