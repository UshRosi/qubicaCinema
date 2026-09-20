using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.Catalog.Domain.Auditoriums;
using QubicaCinema.Catalog.Domain.Movies;
using QubicaCinema.Catalog.Domain.Screenings;

namespace QubicaCinema.Catalog.Infrastructure.Messaging;

/// <summary>
/// Translates what the Screening aggregate recorded into the contracts other services read.
/// </summary>
/// <remarks>
/// A domain event stays inside Catalog and may change whenever the model does; an integration event is a
/// frozen wire contract. Keeping the translation in one place, and free of any database or clock, means the
/// day the model moves on, this is the only file that has to keep the old contract true.
/// <para>
/// Public so that it can be unit tested without a database: everything it needs is passed in.
/// </para>
/// </remarks>
public static class ScreeningIntegrationEvents
{
    /// <summary>
    /// The announcement of a new screening. It carries the auditorium's whole seat map, so Booking needs no
    /// earlier state to sell seats for it.
    /// </summary>
    public static ScreeningScheduled From(ScreeningScheduledDomainEvent domainEvent, Movie movie, Auditorium auditorium) =>
        new(
            domainEvent.ScreeningId,
            domainEvent.MovieId,
            movie.Title,
            domainEvent.AuditoriumId,
            auditorium.Name,
            domainEvent.StartsAt,
            domainEvent.EndsAt,
            domainEvent.PriceAmount,
            domainEvent.PriceCurrency,
            [.. auditorium.Seats
                .OrderBy(seat => seat.Position.Row, StringComparer.Ordinal)
                .ThenBy(seat => seat.Position.Number)
                .Select(seat => new ScreeningSeat(seat.Id, seat.Position.Row, seat.Position.Number))])
        {
            OccurredAt = domainEvent.OccurredAt,
        };

    /// <summary>The announcement that a screening moved or changed price.</summary>
    public static ScreeningRescheduled From(ScreeningRescheduledDomainEvent domainEvent) =>
        new(
            domainEvent.ScreeningId,
            domainEvent.StartsAt,
            domainEvent.EndsAt,
            domainEvent.PriceAmount,
            domainEvent.PriceCurrency)
        {
            OccurredAt = domainEvent.OccurredAt,
        };

    /// <summary>The announcement that a screening was called off.</summary>
    public static ScreeningCancelled From(ScreeningCancelledDomainEvent domainEvent) =>
        new(domainEvent.ScreeningId, domainEvent.Reason)
        {
            OccurredAt = domainEvent.OccurredAt,
        };
}
