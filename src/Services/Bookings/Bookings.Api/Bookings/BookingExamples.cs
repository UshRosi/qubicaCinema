using QubicaCinema.BuildingBlocks.Api.Errors;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Bookings.Domain.Exceptions;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>
/// The worked examples shown for <c>POST /api/v1/bookings</c> in the API reference.
/// </summary>
/// <remarks>
/// Real instances, so a renamed property breaks the build instead of leaving a stale example. The two
/// conflicts are built from the domain exceptions themselves, which means the wording and the extension
/// members (<c>unavailableSeatIds</c>; <c>requested</c> and <c>available</c>) are exactly what a client
/// receives. They are examples and not a schema on purpose: two different bodies share one <c>type</c> URI at
/// 409, and one invented record would document only one of them.
/// </remarks>
internal static class BookingExamples
{
    private static readonly Guid ScreeningId = new("3f1c2a5e-7b1d-4c58-9a35-6d2f0e8b4a11");
    private static readonly Guid SeatA = new("a1b2c3d4-0001-4000-8000-000000000001");
    private static readonly Guid SeatB = new("a1b2c3d4-0001-4000-8000-000000000002");

    /// <summary>Two seats picked on the seat map.</summary>
    internal static CreateBookingRequest ChosenSeats { get; } = new(
        [new BookingItemRequest(ScreeningId, new ExplicitSeatsRequest([SeatA, SeatB]))]);

    /// <summary>A number of seats, left for the system to allocate side by side.</summary>
    internal static CreateBookingRequest ByQuantity { get; } = new(
        [new BookingItemRequest(ScreeningId, new SeatQuantityRequest(2))]);

    /// <summary>A chosen seat that somebody else booked first.</summary>
    internal static ProblemDetails SeatsTaken { get; } = Conflict(new SeatAlreadyBookedException(ScreeningId, [SeatA]));

    /// <summary>A quantity request for which no row has that many free seats together.</summary>
    internal static ProblemDetails NotEnoughAdjacentSeats { get; } =
        Conflict(new NotEnoughAdjacentSeatsException(ScreeningId, requested: 4, available: 6));

    private static ProblemDetails Conflict(DomainException exception)
    {
        var problem = new ProblemDetails
        {
            Type = ProblemTypes.Conflict,
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = exception.Message,
        };

        foreach (KeyValuePair<string, object?> extension in exception.Extensions)
        {
            problem.Extensions[extension.Key] = extension.Value;
        }

        return problem;
    }
}
