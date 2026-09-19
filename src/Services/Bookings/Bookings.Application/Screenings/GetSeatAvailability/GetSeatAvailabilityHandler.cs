using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Exceptions;

namespace QubicaCinema.Bookings.Application.Screenings.GetSeatAvailability;

/// <inheritdoc cref="GetSeatAvailabilityQuery" />
/// <remarks>
/// Reads through the same <see cref="ISeatMapRepository"/> the booking use case does, so "available" has
/// one definition in this service, not a booking one and a display one.
/// </remarks>
public sealed class GetSeatAvailabilityHandler(ISeatMapRepository seatMaps)
    : IQueryHandler<GetSeatAvailabilityQuery, SeatAvailabilityView>
{
    /// <inheritdoc />
    /// <exception cref="ScreeningNotFoundException">No screening has that id.</exception>
    public async Task<SeatAvailabilityView> HandleAsync(
        GetSeatAvailabilityQuery query,
        CancellationToken cancellationToken) =>
        SeatAvailabilityView.From(
            await seatMaps.FindAsync(query.ScreeningId, cancellationToken)
            ?? throw new ScreeningNotFoundException(query.ScreeningId));
}
