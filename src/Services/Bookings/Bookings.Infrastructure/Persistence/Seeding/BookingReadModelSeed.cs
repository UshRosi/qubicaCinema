using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.Infrastructure.Extensions;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;

namespace QubicaCinema.Bookings.Infrastructure.Persistence.Seeding;

/// <summary>
/// A stand-in for the screenings and seats that Catalog's events will provide from chapter 3 on.
/// </summary>
/// <remarks>
/// Booking must be usable on its own before messaging exists, so this writes a small programme straight
/// into the local read model: two auditoriums and two screenings a day in each for three days, laid out
/// relative to the clock so they are always in the future. The ids are generated here and therefore do not
/// match Catalog's — which is acceptable for a stand-in and is exactly what chapter 3 fixes, when this
/// class is deleted and the read model is filled by <c>ScreeningScheduled</c> instead.
/// </remarks>
// TODO(chapter 3): delete this class, and the call in BookingDatabaseInitializer.SeedAsync, once the read
// model is filled from Catalog's ScreeningScheduled events.
internal static class BookingReadModelSeed
{
    private const int DaysProgrammed = 3;

    private static readonly TimeSpan[] ShowTimes = [TimeSpan.FromHours(18), TimeSpan.FromHours(21)];

    private static readonly string[] Titles =
    [
        "Dune: Part Two",
        "The Grand Budapest Hotel",
        "Spirited Away",
        "Heat",
        "La Dolce Vita",
        "Nosferatu",
    ];

    /// <summary>Stages the stand-in read model on the context. The caller commits it.</summary>
    internal static void Write(BookingDbContext context, TimeProvider clock)
    {
        Auditorium[] auditoriums =
        [
            new(Guid.CreateVersion7(), "Sala Rossa", RowCount: 8, SeatsPerRow: 12, Money.Of(11.50m, "EUR")),
            new(Guid.CreateVersion7(), "Sala Piccola", RowCount: 4, SeatsPerRow: 8, Money.Of(7.50m, "EUR")),
        ];

        foreach (Auditorium auditorium in auditoriums)
        {
            context.Seats.AddRange(auditorium.LayOutSeats());
        }

        context.Screenings.AddRange(Programme(auditoriums, clock));
    }

    private static IEnumerable<Screening> Programme(Auditorium[] auditoriums, TimeProvider clock)
    {
        DateTimeOffset firstDay = clock.GetUtcNow().StartOfNextUtcDay();
        int rotation = 0;

        for (int day = 0; day < DaysProgrammed; day++)
        {
            foreach (Auditorium auditorium in auditoriums)
            {
                foreach (TimeSpan showTime in ShowTimes)
                {
                    yield return Screening.Create(
                        Guid.CreateVersion7(),
                        auditorium.Id,
                        auditorium.Name,
                        Titles[rotation++ % Titles.Length],
                        firstDay.AddDays(day) + showTime,
                        auditorium.Price);
                }
            }
        }
    }

    /// <summary>
    /// The shape of a stand-in auditorium. Booking has no auditorium of its own — only the seats and the
    /// name a screening carries — so this exists only to lay out the demo data.
    /// </summary>
    private sealed record Auditorium(Guid Id, string Name, int RowCount, int SeatsPerRow, Money Price)
    {
        public IEnumerable<Seat> LayOutSeats() =>
            from row in Enumerable.Range(0, RowCount)
            from number in Enumerable.Range(1, SeatsPerRow)
            select Seat.Create(Guid.CreateVersion7(), Id, SeatPosition.Of(((char)('A' + row)).ToString(), number));
    }
}
