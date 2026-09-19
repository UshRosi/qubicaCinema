using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Bookings.Application.Abstractions.Queries;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Domain.SeatAllocation;

namespace QubicaCinema.Bookings.Application.Bookings.CreateBooking;

/// <inheritdoc cref="CreateBookingCommand" />
/// <remarks>
/// The handler gathers and delegates. The seat map decides which seats a selection means, the booking
/// decides whether they can be booked, <see cref="BookingPlacement"/> decides whether a lost race is worth
/// another try, and the database's unique index has the last word on who got the seat.
/// </remarks>
public sealed class CreateBookingHandler(
    ISeatMapRepository seatMaps,
    ISeatAllocationStrategy allocationStrategy,
    BookingPlacement placement,
    IBookingQueries queries,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<CreateBookingCommand, Versioned<BookingView>>
{
    /// <inheritdoc />
    /// <exception cref="ScreeningNotFoundException">A screening is unknown.</exception>
    /// <exception cref="SeatNotFoundException">A named seat is not in the screening's auditorium.</exception>
    /// <exception cref="SeatAlreadyBookedException">A seat was taken, and could not be chosen again.</exception>
    /// <exception cref="NotEnoughAdjacentSeatsException">No row has enough free seats side by side.</exception>
    /// <exception cref="ScreeningCancelledException">A screening was called off.</exception>
    /// <exception cref="ScreeningStartedException">A screening has already begun.</exception>
    public async Task<Versioned<BookingView>> HandleAsync(
        CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        Booking booking = await placement.PlaceAsync(
            attemptToken => ComposeAsync(command, attemptToken),
            conflict => command.AllocatesAutomatically(conflict.ScreeningId),
            cancellationToken);

        return await queries.FindAsync(booking.Id, cancellationToken)
               ?? throw new InvalidOperationException($"Booking {booking.Id} was committed but cannot be read back.");
    }

    /// <summary>Reads every seat map afresh and builds the booking from it.</summary>
    private async Task<Booking> ComposeAsync(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        List<SeatClaim> claims = [];

        foreach (ScreeningSelection item in command.Items)
        {
            SeatMap seatMap = await seatMaps.FindAsync(item.ScreeningId, cancellationToken)
                              ?? throw new ScreeningNotFoundException(item.ScreeningId);

            claims.Add(seatMap.Claim(item.Selection, allocationStrategy));
        }

        return Booking.Create(currentUser.UserId, claims, clock);
    }
}
