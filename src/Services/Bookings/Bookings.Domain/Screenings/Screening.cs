using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Screenings;

/// <summary>
/// Booking's own copy of a screening that Catalog owns: what it needs to sell seats for it.
/// </summary>
/// <remarks>
/// Not Catalog's aggregate and not a reference into Catalog's database. Each service keeps the data it
/// decides with, so a booking can be made while Catalog is down, and the price a customer pays is the one
/// this service knew when it took the booking. The id is Catalog's, so both sides talk about the same
/// screening; the copy is kept current by Catalog's events from chapter 3 on.
/// <para>
/// It carries the movie title and the auditorium name, denormalised, because the seat map shows them and
/// Booking has no movies or auditoriums of its own to join to.
/// </para>
/// </remarks>
public sealed class Screening : AggregateRoot<Guid>
{
    private Screening(
        Guid id,
        Guid auditoriumId,
        string auditoriumName,
        string movieTitle,
        DateTimeOffset startsAt,
        Money price)
        : base(id)
    {
        AuditoriumId = auditoriumId;
        AuditoriumName = auditoriumName;
        MovieTitle = movieTitle;
        StartsAt = startsAt;
        Price = price;
        Status = ScreeningStatus.Scheduled;
    }

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private Screening()
    {
        AuditoriumName = string.Empty;
        MovieTitle = string.Empty;
        Price = null!;
    }

    /// <summary>The auditorium it takes place in; its seats are the seats that can be booked.</summary>
    public Guid AuditoriumId { get; private set; }

    /// <summary>The auditorium's name, as Catalog announced it.</summary>
    public string AuditoriumName { get; private set; }

    /// <summary>The movie's title, as Catalog announced it.</summary>
    public string MovieTitle { get; private set; }

    /// <summary>When the audience is admitted. After this, its seats can be neither booked nor given back.</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>What one seat costs. Copied into each booking item, so a later price change never rewrites a booking.</summary>
    public Money Price { get; private set; }

    /// <summary>Whether it is still going to happen.</summary>
    public ScreeningStatus Status { get; private set; }

    /// <summary>Records a screening that Catalog has put on the programme.</summary>
    /// <param name="id">Catalog's id for the screening.</param>
    /// <param name="auditoriumId">Catalog's id for the auditorium.</param>
    /// <param name="auditoriumName">The auditorium's name.</param>
    /// <param name="movieTitle">The movie's title.</param>
    /// <param name="startsAt">When the audience is admitted.</param>
    /// <param name="price">What one seat costs.</param>
    public static Screening Create(
        Guid id,
        Guid auditoriumId,
        string auditoriumName,
        string movieTitle,
        DateTimeOffset startsAt,
        Money price) =>
        new(id, auditoriumId, auditoriumName, movieTitle, startsAt, price);

    /// <summary>
    /// Whether it is under way or over at the given instant. A cancelled screening never starts, so it
    /// never counts as started — which is what lets a booking for it still be cancelled.
    /// </summary>
    public bool HasStartedBy(DateTimeOffset instant) =>
        Status == ScreeningStatus.Scheduled && instant >= StartsAt;

    /// <summary>Refuses a booking for a screening that is cancelled or has already begun.</summary>
    /// <exception cref="ScreeningCancelledException">It was called off.</exception>
    /// <exception cref="ScreeningStartedException">It has already begun.</exception>
    public void EnsureOpenForBooking(DateTimeOffset now)
    {
        if (Status == ScreeningStatus.Cancelled)
        {
            throw new ScreeningCancelledException(Id);
        }

        if (HasStartedBy(now))
        {
            throw new ScreeningStartedException(Id, StartsAt);
        }
    }
}
