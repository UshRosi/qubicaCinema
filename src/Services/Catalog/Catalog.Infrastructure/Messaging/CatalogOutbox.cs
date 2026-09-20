using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.Contracts;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.BuildingBlocks.Persistence.Outbox;
using QubicaCinema.Catalog.Domain.Auditoriums;
using QubicaCinema.Catalog.Domain.Movies;
using QubicaCinema.Catalog.Domain.Screenings;
using QubicaCinema.Catalog.Infrastructure.Persistence;

namespace QubicaCinema.Catalog.Infrastructure.Messaging;

/// <summary>
/// Turns the domain events the tracked aggregates recorded into outbox rows, inside the unit of work that
/// changed them.
/// </summary>
/// <remarks>
/// Called from <see cref="CatalogDbContext.SaveChangesAsync"/> just before the save, so an event row and the
/// change it announces are written by one transaction — the guarantee the whole outbox exists to give. It
/// runs from the context rather than from an interceptor or a registered service because it needs this
/// context's own change tracker, including entities that have been added but not saved yet: the seed writes
/// a movie, an auditorium and their screenings in one save, and the screening's announcement needs the
/// movie's title and the auditorium's seats from that same, unsaved, batch.
/// <para>
/// No handler ever sees the event bus. A handler that published directly would escape the transaction.
/// </para>
/// </remarks>
internal static class CatalogOutbox
{
    internal static async Task StageAsync(CatalogDbContext context, CancellationToken cancellationToken)
    {
        Screening[] changed =
        [
            .. context.ChangeTracker.Entries<Screening>()
                .Select(entry => entry.Entity)
                .Where(screening => screening.DomainEvents.Count > 0),
        ];

        foreach (Screening screening in changed)
        {
            foreach (IDomainEvent domainEvent in screening.DomainEvents)
            {
                IntegrationEvent integrationEvent = await TranslateAsync(context, domainEvent, cancellationToken);

                context.Set<OutboxMessage>().Add(OutboxMessage.From(integrationEvent, Activity.Current));
            }

            // Cleared now that they are staged: a second save on this context must not announce them again.
            screening.ClearDomainEvents();
        }
    }

    private static async Task<IntegrationEvent> TranslateAsync(
        CatalogDbContext context,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken) =>
        domainEvent switch
        {
            ScreeningScheduledDomainEvent scheduled => await TranslateScheduledAsync(context, scheduled, cancellationToken),
            ScreeningRescheduledDomainEvent rescheduled => ScreeningIntegrationEvents.From(rescheduled),
            ScreeningCancelledDomainEvent cancelled => ScreeningIntegrationEvents.From(cancelled),

            // A new domain event that nobody mapped is a programming error, and it must fail loudly here:
            // silently dropping it would mean an announcement that never reaches anyone.
            _ => throw new InvalidOperationException(
                $"{domainEvent.GetType().Name} has no integration event. Map it in {nameof(ScreeningIntegrationEvents)}."),
        };

    private static async Task<IntegrationEvent> TranslateScheduledAsync(
        CatalogDbContext context,
        ScreeningScheduledDomainEvent scheduled,
        CancellationToken cancellationToken)
    {
        // Find looks in the change tracker first, so an unsaved movie or auditorium is found without a query.
        Movie movie = await context.Movies.FindAsync([scheduled.MovieId], cancellationToken)
                      ?? throw new InvalidOperationException($"Movie {scheduled.MovieId} of a scheduled screening does not exist.");

        Auditorium auditorium = await context.Auditoriums.FindAsync([scheduled.AuditoriumId], cancellationToken)
                                ?? throw new InvalidOperationException(
                                    $"Auditorium {scheduled.AuditoriumId} of a scheduled screening does not exist.");

        // The API loads no seats to schedule a screening, but the announcement needs every one. An auditorium
        // that has just been added already holds its seats in memory, and cannot be loaded from a database
        // that does not have it yet.
        var seats = context.Entry(auditorium).Collection(room => room.Seats);

        if (seats.EntityEntry.State != EntityState.Added && !seats.IsLoaded)
        {
            await seats.LoadAsync(cancellationToken);
        }

        return ScreeningIntegrationEvents.From(scheduled, movie, auditorium);
    }
}
