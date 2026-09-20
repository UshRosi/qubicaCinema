using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Bookings;

/// <summary>
/// One customer's purchase: seats at one or more screenings, bought together and given back together or
/// one at a time.
/// </summary>
/// <remarks>
/// The aggregate is the unit of consistency for what a customer holds. Its items change only through its
/// own methods, and the rules that span them — every seat is for a screening still open, a seat is never
/// in it twice, cancelling the last seat cancels the booking, the total counts only seats still held — are
/// stated here once and unit tested without a database.
/// <para>
/// What it cannot know on its own is whether a seat is free: that depends on every other booking. It is
/// handed seats already checked against a <see cref="SeatMap"/>, and the database's filtered unique index
/// is the final word when two bookings race for the same seat.
/// </para>
/// </remarks>
public sealed class Booking : AggregateRoot<Guid>
{
    /// <summary>How many screenings one booking may span. A basket, not a season ticket.</summary>
    public const int MaxScreenings = 20;

    private readonly List<BookingItem> _items = [];

    private Booking(Guid id, Guid userId, DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        CreatedAt = createdAt;
        Status = BookingStatus.Confirmed;
    }

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private Booking()
    {
    }

    /// <summary>The customer who made it, by their Identity id.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Whether it still holds any seat.</summary>
    public BookingStatus Status { get; private set; }

    /// <summary>When it was made.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When its last seat was given back, or null while it holds any.</summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>Every item ever booked, cancelled ones included. Read-only: items change only through the booking.</summary>
    public IReadOnlyCollection<BookingItem> Items => _items.AsReadOnly();

    /// <summary>The items that still hold a seat.</summary>
    public IEnumerable<BookingItem> ActiveItems => _items.Where(item => item.IsActive);

    /// <summary>The screenings its items are for, each once.</summary>
    public IReadOnlyCollection<Guid> ScreeningIds => [.. _items.Select(item => item.ScreeningId).Distinct()];

    /// <summary>
    /// What the seats still held cost. Cancelled items are not refunded twice and not charged: they do not
    /// count.
    /// </summary>
    /// <remarks>
    /// Computed here and nowhere else. A total worked out in a mapper or a SQL projection is a second
    /// definition, and the day one of them forgets to skip cancelled items the two disagree.
    /// </remarks>
    public Money Total => ActiveItems.Aggregate(Money.Zero(Currency), (sum, item) => sum.Add(item.Price));

    /// <summary>The currency the booking is priced in. Every item shares it; <see cref="Create"/> enforces that.</summary>
    private string Currency => _items[0].Price.Currency;

    /// <summary>Books the claimed seats for a customer.</summary>
    /// <param name="userId">The customer.</param>
    /// <param name="claims">The seats chosen at each screening.</param>
    /// <param name="clock">The clock, so that "already started" is testable.</param>
    /// <exception cref="InvalidBookingException">No seats, too many screenings, or a seat twice.</exception>
    /// <exception cref="SeatNotFoundException">A seat is not in the screening's auditorium.</exception>
    /// <exception cref="ScreeningCancelledException">A screening was called off.</exception>
    /// <exception cref="ScreeningStartedException">A screening has already begun.</exception>
    /// <exception cref="CurrencyMismatchException">The screenings are priced in different currencies.</exception>
    public static Booking Create(Guid userId, IReadOnlyCollection<SeatClaim> claims, TimeProvider clock)
    {
        EnsureWellFormed(claims);

        DateTimeOffset now = clock.GetUtcNow();
        var booking = new Booking(Guid.CreateVersion7(), userId, now);

        foreach (SeatClaim claim in claims)
        {
            claim.Screening.EnsureOpenForBooking(now);
            EnsureSeatsAreInTheAuditorium(claim);

            foreach (Seat seat in claim.Seats)
            {
                booking._items.Add(new BookingItem(Guid.CreateVersion7(), claim.Screening.Id, seat.Id, claim.Screening.Price));
            }
        }

        // Summing is what reveals a second currency, and it is better revealed now than on the first read.
        _ = booking.Total;

        return booking;
    }

    /// <summary>Whether this customer made the booking.</summary>
    public bool IsOwnedBy(Guid userId) => UserId == userId;

    /// <summary>
    /// Gives back every seat still held. The booking is kept, readable, with status
    /// <see cref="BookingStatus.Cancelled"/>.
    /// </summary>
    /// <remarks>
    /// All or nothing: if any seat is for a screening that has started, nothing is cancelled, so a customer
    /// never ends up with half a booking they did not ask for. They can still give back the seats for later
    /// screenings one at a time. Idempotent: a cancelled booking stays as it is and no error is raised, so a
    /// client that retries a timed-out request gets the same answer.
    /// </remarks>
    /// <param name="screenings">The screenings of the booking's items, to know which have started.</param>
    /// <param name="clock">The clock, so that "already started" is testable.</param>
    /// <exception cref="BookingNotCancellableException">A seat is for a screening that has started.</exception>
    public void Cancel(IEnumerable<Screening> screenings, TimeProvider clock)
    {
        if (Status == BookingStatus.Cancelled)
        {
            return;
        }

        DateTimeOffset now = clock.GetUtcNow();
        var schedule = screenings.ToDictionary(screening => screening.Id);
        BookingItem[] held = [.. ActiveItems];

        foreach (BookingItem item in held)
        {
            EnsureNotStarted(item, schedule, now);
        }

        foreach (BookingItem item in held)
        {
            item.Cancel(now);
        }

        MarkCancelled(now);
    }

    /// <summary>
    /// Gives back one seat. Giving back the last seat still held cancels the booking, because a confirmed
    /// booking that holds nothing is a state no customer or report should ever have to interpret.
    /// </summary>
    /// <remarks>Idempotent, like <see cref="Cancel"/>: cancelling a cancelled item changes nothing.</remarks>
    /// <param name="itemId">The item to give back.</param>
    /// <param name="screenings">The screenings of the booking's items, to know which have started.</param>
    /// <param name="clock">The clock, so that "already started" is testable.</param>
    /// <exception cref="BookingItemNotFoundException">The booking has no such item.</exception>
    /// <exception cref="BookingNotCancellableException">The seat is for a screening that has started.</exception>
    public void CancelItem(Guid itemId, IEnumerable<Screening> screenings, TimeProvider clock)
    {
        BookingItem item = _items.Find(candidate => candidate.Id == itemId)
                           ?? throw new BookingItemNotFoundException(Id, itemId);

        if (!item.IsActive)
        {
            return;
        }

        DateTimeOffset now = clock.GetUtcNow();
        EnsureNotStarted(item, screenings.ToDictionary(screening => screening.Id), now);

        item.Cancel(now);

        if (!ActiveItems.Any())
        {
            MarkCancelled(now);
        }
    }

    /// <summary>
    /// Gives back every seat held at one screening, because the cinema called that screening off. A booking
    /// left holding nothing is cancelled.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Cancel"/> this is not the customer's choice, so it never refuses: a screening that
    /// is called off has no start time to be "too late" for. Idempotent — a booking with nothing held at that
    /// screening is left exactly as it was, which is what makes a redelivered event harmless.
    /// </remarks>
    /// <param name="screeningId">The screening that was cancelled.</param>
    /// <param name="clock">The clock, for the time the seats were given back.</param>
    public void ReleaseSeatsFor(Guid screeningId, TimeProvider clock)
    {
        BookingItem[] held = [.. ActiveItems.Where(item => item.ScreeningId == screeningId)];

        if (held.Length == 0)
        {
            return;
        }

        DateTimeOffset now = clock.GetUtcNow();

        foreach (BookingItem item in held)
        {
            item.Cancel(now);
        }

        if (!ActiveItems.Any())
        {
            MarkCancelled(now);
        }
    }

    private void MarkCancelled(DateTimeOffset now)
    {
        Status = BookingStatus.Cancelled;
        CancelledAt = now;
    }

    private void EnsureNotStarted(BookingItem item, Dictionary<Guid, Screening> schedule, DateTimeOffset now)
    {
        if (!schedule.TryGetValue(item.ScreeningId, out Screening? screening))
        {
            // A caller bug, not a business rule: the use case must load every screening the booking uses.
            throw new InvalidOperationException(
                $"Screening {item.ScreeningId} of booking {Id} was not supplied, so it cannot be checked.");
        }

        if (screening.HasStartedBy(now))
        {
            throw new BookingNotCancellableException(Id, screening.Id, screening.StartsAt);
        }
    }

    private static void EnsureWellFormed(IReadOnlyCollection<SeatClaim> claims)
    {
        if (claims.Count == 0 || claims.Any(claim => claim.Seats.Count == 0))
        {
            throw new InvalidBookingException("A booking must hold at least one seat at every screening it names.");
        }

        if (claims.Select(claim => claim.Screening.Id).Distinct().Count() > MaxScreenings)
        {
            throw new InvalidBookingException($"A booking may span at most {MaxScreenings} screenings.");
        }

        bool seatTwice = claims
            .SelectMany(claim => claim.Seats.Select(seat => (claim.Screening.Id, seat.Id)))
            .GroupBy(pair => pair)
            .Any(group => group.Count() > 1);

        if (seatTwice)
        {
            throw new InvalidBookingException("A booking cannot hold the same seat at the same screening twice.");
        }
    }

    private static void EnsureSeatsAreInTheAuditorium(SeatClaim claim)
    {
        Guid[] strays = [.. claim.Seats
            .Where(seat => seat.AuditoriumId != claim.Screening.AuditoriumId)
            .Select(seat => seat.Id)];

        if (strays.Length > 0)
        {
            throw new SeatNotFoundException(claim.Screening.Id, strays);
        }
    }
}
