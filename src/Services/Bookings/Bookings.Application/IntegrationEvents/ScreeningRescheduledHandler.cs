using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.BuildingBlocks.EventBus;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.IntegrationEvents;

/// <summary>Applies a new time and price that Catalog announced to Booking's copy of the screening.</summary>
/// <remarks>
/// Without it Booking would keep selling seats at the old time and the old price. Bookings already taken
/// keep the price they were made at: each item snapshots it.
/// <para>
/// A screening Booking has never heard of is an error, not something to skip: the publisher sends events in
/// order, so it can only mean a message is in flight out of turn. Failing makes the broker redeliver it,
/// by which time the announcement has normally arrived.
/// </para>
/// </remarks>
public sealed class ScreeningRescheduledHandler(IScreeningRepository screenings)
    : IIntegrationHandler<ScreeningRescheduled>
{
    /// <inheritdoc />
    /// <exception cref="ScreeningNotFoundException">The screening was never announced here.</exception>
    public async Task HandleAsync(ScreeningRescheduled integrationEvent, CancellationToken cancellationToken)
    {
        Screening screening = await screenings.FindAsync(integrationEvent.ScreeningId, cancellationToken)
                              ?? throw new ScreeningNotFoundException(integrationEvent.ScreeningId);

        screening.Reschedule(
            integrationEvent.StartsAt,
            Money.Of(integrationEvent.PriceAmount, integrationEvent.PriceCurrency));
    }
}
