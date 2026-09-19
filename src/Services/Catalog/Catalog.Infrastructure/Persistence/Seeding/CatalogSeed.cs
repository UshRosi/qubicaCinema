using QubicaCinema.Catalog.Domain.Auditoriums;
using QubicaCinema.Catalog.Domain.Movies;
using QubicaCinema.Catalog.Domain.Screenings;
using QubicaCinema.Catalog.Domain.ValueObjects;
using QubicaCinema.Catalog.Infrastructure.Extensions;

namespace QubicaCinema.Catalog.Infrastructure.Persistence.Seeding;

/// <summary>
/// The demo catalogue: three rooms, six films and a week of programming.
/// </summary>
/// <remarks>
/// Built entirely through the aggregates, so every rule in the model applies to it. The programme is laid
/// out relative to the clock rather than around fixed dates, which means the seeded screenings are always
/// in the future — a hard-coded date would quietly turn the whole demo into "screening already started"
/// the week after it was written.
/// </remarks>
internal static class CatalogSeed
{
    /// <summary>When the first film of the day is admitted.</summary>
    private static readonly TimeSpan FirstShow = TimeSpan.FromHours(15);

    /// <summary>Start times are rounded up to this, the way a printed programme does.</summary>
    private static readonly TimeSpan ShowTimeGranularity = TimeSpan.FromMinutes(15);

    /// <summary>How many films each auditorium shows per day.</summary>
    private const int ShowsPerDay = 3;

    /// <summary>How many days of programme to lay out.</summary>
    private const int DaysProgrammed = 7;

    /// <summary>Stages the whole catalogue on the context. The caller commits it.</summary>
    internal static void Write(CatalogDbContext context, TimeProvider clock)
    {
        Auditorium[] auditoriums =
        [
            Auditorium.Create("Sala Rossa", rowCount: 8, seatsPerRow: 12),
            Auditorium.Create("Sala Blu", rowCount: 6, seatsPerRow: 10),
            Auditorium.Create("Sala Piccola", rowCount: 4, seatsPerRow: 8),
        ];

        Movie[] movies =
        [
            Movie.Create("Dune: Part Two", "Paul Atreides unites with the Fremen to avenge his family.", TimeSpan.FromMinutes(166), MovieGenre.ScienceFiction, AgeRating.Teen),
            Movie.Create("The Grand Budapest Hotel", "A concierge and his lobby boy inherit a fortune and a murder charge.", TimeSpan.FromMinutes(99), MovieGenre.Comedy, AgeRating.ParentalGuidance),
            Movie.Create("Spirited Away", "A girl wanders into a world of spirits and must win back her parents.", TimeSpan.FromMinutes(125), MovieGenre.Animation, AgeRating.General),
            Movie.Create("Heat", "A detective and a career thief measure each other across one city.", TimeSpan.FromMinutes(170), MovieGenre.Thriller, AgeRating.Adult),
            Movie.Create("La Dolce Vita", "A week in the life of a gossip columnist in Rome.", TimeSpan.FromMinutes(174), MovieGenre.Drama, AgeRating.Teen),
            Movie.Create("Nosferatu", "A vampire arrives by sea, and a plague arrives with him.", TimeSpan.FromMinutes(132), MovieGenre.Horror, AgeRating.Adult),
        ];

        context.Auditoriums.AddRange(auditoriums);
        context.Movies.AddRange(movies);
        context.Screenings.AddRange(Programme(auditoriums, movies, clock));
    }

    /// <summary>
    /// Lays out the week: each auditorium shows films back to back from the afternoon on, rotating through
    /// the catalogue.
    /// </summary>
    /// <remarks>
    /// Each show starts when the room is actually free again — the previous film's end, cleaning included,
    /// rounded up to the next quarter hour — rather than at fixed hours. Fixed hours only work while every
    /// film is short enough to fit between them, and the first three-hour film in the catalogue turns the
    /// seed into an overlap the model rightly refuses. Deriving the times from the films means the demo
    /// programme stays valid whatever is added to it.
    /// <para>
    /// The slots each auditorium has already been given are carried along and handed to
    /// <see cref="Screening.Schedule"/>, exactly as the real use case does it, so the seed goes through the
    /// same invariant as a request would.
    /// </para>
    /// </remarks>
    private static IEnumerable<Screening> Programme(Auditorium[] auditoriums, Movie[] movies, TimeProvider clock)
    {
        DateTimeOffset firstDay = clock.GetUtcNow().StartOfNextUtcDay();
        var committed = auditoriums.ToDictionary(auditorium => auditorium.Id, _ => new List<ScheduledSlot>());
        int rotation = 0;

        for (int day = 0; day < DaysProgrammed; day++)
        {
            foreach (Auditorium auditorium in auditoriums)
            {
                DateTimeOffset freeFrom = firstDay.AddDays(day) + FirstShow;

                for (int show = 0; show < ShowsPerDay; show++)
                {
                    Movie movie = movies[rotation++ % movies.Length];

                    var screening = Screening.Schedule(
                        movie.Id,
                        movie.Duration,
                        auditorium.Id,
                        freeFrom.RoundUpTo(ShowTimeGranularity),
                        PriceFor(auditorium),
                        committed[auditorium.Id],
                        clock);

                    committed[auditorium.Id].Add(new ScheduledSlot(screening.Id, screening.Slot));
                    freeFrom = screening.Slot.EndsAt;

                    yield return screening;
                }
            }
        }
    }

    /// <summary>A bigger room costs more, so the demo data has something to sort and filter by.</summary>
    private static Money PriceFor(Auditorium auditorium) => auditorium.Capacity switch
    {
        >= 90 => Money.Of(11.50m, "EUR"),
        >= 50 => Money.Of(9.50m, "EUR"),
        _ => Money.Of(7.50m, "EUR"),
    };
}
