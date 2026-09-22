using System.ComponentModel;
using QubicaCinema.BuildingBlocks.Api.Concurrency;
using QubicaCinema.BuildingBlocks.Api.Endpoints;
using QubicaCinema.BuildingBlocks.Api.Idempotency;
using QubicaCinema.BuildingBlocks.Api.OpenApi;
using QubicaCinema.BuildingBlocks.Api.Paging;
using QubicaCinema.BuildingBlocks.Api.Validation;
using QubicaCinema.BuildingBlocks.Authentication;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Bookings.Application.Bookings;
using QubicaCinema.Bookings.Application.Bookings.CancelBooking;
using QubicaCinema.Bookings.Application.Bookings.CancelBookingItem;
using QubicaCinema.Bookings.Application.Bookings.CreateBooking;
using QubicaCinema.Bookings.Application.Bookings.GetBooking;
using QubicaCinema.Bookings.Application.Bookings.GetBookings;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>The bookings: <c>/api/v1/bookings</c>.</summary>
/// <remarks>
/// Who may see which booking is decided in the use cases, not here, because it needs the loaded booking.
/// The routes only require a token. Booking needs the <c>Customer</c> role, because an administrator does not
/// book for themselves; everything else needs merely <c>authenticated</c>, and not <c>Customer</c>, so that an
/// administrator reaches the use cases and their "may act for the cinema" rule instead of a 403 before it.
/// </remarks>
internal sealed class BookingEndpoints : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder bookings = endpoints.MapGroup("/bookings").WithTags("Bookings");

        bookings.MapGet("/", ListAsync)
            .RequiringPolicy(CinemaPolicies.Authenticated)
            .WithName("ListBookings")
            .WithSummary("Lists the caller's bookings, newest first. An administrator may list anyone's.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        bookings.MapGet("/{id:guid}", GetAsync)
            .RequiringPolicy(CinemaPolicies.Authenticated)
            .WithName("GetBooking")
            .WithSummary("Returns one booking, with its ETag.")
            .WithETagHeader()
            .ProducesProblem(StatusCodes.Status404NotFound);

        bookings.MapPost("/", CreateAsync)
            .RequiringPolicy(CinemaPolicies.Customer)
            // Validation first: a request that is wrong on its face must not claim an idempotency key.
            .ValidatingBody<CreateBookingRequest>()
            .RequiringIdempotencyKey<CreateBookingRequest>()
            .WithName("CreateBooking")
            .WithSummary("Books chosen seats, or a number of seats, at one or more screenings.")
            .WithLocationHeader()
            .WithETagHeader(StatusCodes.Status201Created)
            .WithRequestExample("chosen-seats", "Seats picked on the seat map", BookingExamples.ChosenSeats)
            .WithRequestExample("by-quantity", "A number of seats, allocated for you", BookingExamples.ByQuantity)
            .WithResponseExample(
                StatusCodes.Status409Conflict,
                "seats-taken",
                "A chosen seat was booked by somebody else first",
                BookingExamples.SeatsTaken)
            .WithResponseExample(
                StatusCodes.Status409Conflict,
                "not-enough-adjacent-seats",
                "No row has that many free seats together",
                BookingExamples.NotEnoughAdjacentSeats)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // A cancelled booking is still readable, so DELETE would promise something this service does not
        // do. The cancellation is its own sub-resource, and asking twice is not an error.
        bookings.MapPost("/{id:guid}/cancellation", CancelAsync)
            .RequiringPolicy(CinemaPolicies.Authenticated)
            .WithName("CancelBooking")
            .WithSummary("Gives back every seat of a booking. The booking stays readable, as Cancelled.")
            .WithETagHeader()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed);

        bookings.MapPost("/{id:guid}/items/{itemId:guid}/cancellation", CancelItemAsync)
            .RequiringPolicy(CinemaPolicies.Authenticated)
            .WithName("CancelBookingItem")
            .WithSummary("Gives back one seat. Giving back the last seat cancels the booking.")
            .WithETagHeader()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed);
    }

    private static async Task<Ok<PagedResponse<BookingView>>> ListAsync(
        [AsParameters] PageQuery page,
        [Description("Administrators only: list this user's bookings instead of the caller's.")] Guid? userId,
        GetBookingsHandler handler,
        CancellationToken cancellationToken)
    {
        PagedResult<BookingView> result =
            await handler.HandleAsync(new GetBookingsQuery(userId, page.Skip, page.Size), cancellationToken);

        return TypedResults.Ok(result.ToResponse(page));
    }

    private static async Task<Ok<BookingView>> GetAsync(
        Guid id,
        GetBookingHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        Versioned(await handler.HandleAsync(new GetBookingQuery(id), cancellationToken), response);

    private static async Task<Created<BookingView>> CreateAsync(
        CreateBookingRequest request,
        CreateBookingHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        Versioned<BookingView> booking = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.SetETag(booking.Version);

        return TypedResults.Created($"/api/v1/bookings/{booking.Value.Id}", booking.Value);
    }

    private static async Task<Ok<BookingView>> CancelAsync(
        Guid id,
        CancelBookingHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        Versioned(await handler.HandleAsync(new CancelBookingCommand(id), cancellationToken), response);

    private static async Task<Ok<BookingView>> CancelItemAsync(
        Guid id,
        Guid itemId,
        CancelBookingItemHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        Versioned(await handler.HandleAsync(new CancelBookingItemCommand(id, itemId), cancellationToken), response);

    /// <summary>Answers with the booking and puts its version in the <c>ETag</c> header.</summary>
    private static Ok<BookingView> Versioned(Versioned<BookingView> booking, HttpResponse response)
    {
        response.SetETag(booking.Version);

        return TypedResults.Ok(booking.Value);
    }
}
