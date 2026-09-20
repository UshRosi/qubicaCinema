using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.EventBus;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.IntegrationEvents;

/// <summary>
/// Marks a screening as cancelled and gives back every seat booked for it.
/// </summary>
/// <remarks>
/// A confirmed booking for a screening that will never happen is a state nobody can explain, so the
/// consumer resolves it: the seats are released and a booking left holding nothing is cancelled.
/// Booking publishes no event of its own about this. Doing so from inside a handler would be a second write
/// outside the transaction — the very dual-write the outbox exists to avoid — and nothing consumes it yet.
/// If a notification service ever needs it, Booking gets an outbox of its own.
/// </remarks>
public sealed class ScreeningCancelledHandler(
    IScreeningRepository screenings,
    IBookingRepository bookings,
    TimeProvider clock) : IIntegrationHandler<ScreeningCancelled>
{
    /// <inheritdoc />
    /// <exception cref="ScreeningNotFoundException">The screening was never announced here.</exception>
    public async Task HandleAsync(ScreeningCancelled integrationEvent, CancellationToken cancellationToken)
    {
        Screening screening = await screenings.FindAsync(integrationEvent.ScreeningId, cancellationToken)
                              ?? throw new ScreeningNotFoundException(integrationEvent.ScreeningId);

        screening.Cancel();

        IReadOnlyCollection<Booking> affected =
            await bookings.GetHoldingSeatsForAsync(integrationEvent.ScreeningId, cancellationToken);

        foreach (Booking booking in affected)
        {
            booking.ReleaseSeatsFor(integrationEvent.ScreeningId, clock);
        }
    }
}
