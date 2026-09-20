using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.BuildingBlocks.EventBus;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Application.IntegrationEvents;

/// <summary>
/// Adds a screening Catalog announced to Booking's own copy of the programme, with the seats it needs.
/// </summary>
/// <remarks>
/// The event is self-contained, so no earlier state is required and replaying the whole stream from the
/// start gives the same result. Two things make it safe to see the same screening twice. The inbox drops a
/// redelivery of one event; the existence check here covers the rarer case of the same screening arriving
/// in a second event, for instance when an operator republishes it.
/// </remarks>
public sealed class ScreeningScheduledHandler(
    IScreeningRepository screenings,
    ISeatRepository seats) : IIntegrationHandler<ScreeningScheduled>
{
    /// <inheritdoc />
    public async Task HandleAsync(ScreeningScheduled integrationEvent, CancellationToken cancellationToken)
    {
        if (await screenings.FindAsync(integrationEvent.ScreeningId, cancellationToken) is not null)
        {
            return;
        }

        // The seats belong to the auditorium, so a second screening in the same room brings none that are new.
        IReadOnlySet<Guid> known = await seats.GetKnownIdsAsync(
            [.. integrationEvent.Seats.Select(seat => seat.SeatId)],
            cancellationToken);

        seats.AddRange(integrationEvent.Seats
            .Where(seat => !known.Contains(seat.SeatId))
            .Select(seat => Seat.Create(
                seat.SeatId,
                integrationEvent.AuditoriumId,
                SeatPosition.Of(seat.Row, seat.Number))));

        screenings.Add(Screening.Create(
            integrationEvent.ScreeningId,
            integrationEvent.AuditoriumId,
            integrationEvent.AuditoriumName,
            integrationEvent.MovieTitle,
            integrationEvent.StartsAt,
            Money.Of(integrationEvent.PriceAmount, integrationEvent.PriceCurrency)));
    }
}
