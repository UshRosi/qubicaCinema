using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.ValueObjects;

namespace QubicaCinema.Catalog.Domain.Screenings;

/// <summary>
/// One showing of one movie, in one auditorium, at one time.
/// </summary>
/// <remarks>
/// The aggregate that owns the cinema's central rule: an auditorium cannot show two things at once. The
/// check lives here, not in a handler and not in a database constraint, because it is a statement about the
/// business and it has to be testable without either.
/// <para>
/// The screening refers to its movie and its auditorium by id. Holding the objects would let a caller edit
/// a movie through a screening and would drag two more aggregates into every load; what a screening really
/// needs from the movie is its running time, and it copies that once, when it is scheduled.
/// </para>
/// </remarks>
public sealed class Screening : AggregateRoot<Guid>
{
    /// <summary>
    /// Time reserved after the film ends, before the auditorium can be used again.
    /// </summary>
    /// <remarks>
    /// Without it the model would happily schedule a film that starts the second the previous one ends,
    /// which no cinema can actually do. It makes the overlap rule mean "the room is free", not "the film
    /// is over".
    /// </remarks>
    public static readonly TimeSpan CleaningBuffer = TimeSpan.FromMinutes(20);

    private Screening(Guid id, Guid movieId, Guid auditoriumId, TimeSlot slot, Money price)
        : base(id)
    {
        MovieId = movieId;
        AuditoriumId = auditoriumId;
        Slot = slot;
        Price = price;
        Status = ScreeningStatus.Scheduled;
    }

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private Screening()
    {
        Slot = null!;
        Price = null!;
    }

    /// <summary>The movie being shown.</summary>
    public Guid MovieId { get; private set; }

    /// <summary>The auditorium it occupies.</summary>
    public Guid AuditoriumId { get; private set; }

    /// <summary>
    /// When the auditorium is busy: the film's running time plus <see cref="CleaningBuffer"/>.
    /// The audience is admitted at <see cref="TimeSlot.StartsAt"/>.
    /// </summary>
    public TimeSlot Slot { get; private set; }

    /// <summary>What one seat at this screening costs.</summary>
    public Money Price { get; private set; }

    /// <summary>Whether it is still going to happen.</summary>
    public ScreeningStatus Status { get; private set; }

    /// <summary>When it was called off, or null while it is still scheduled.</summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>Why it was called off, when a reason was given.</summary>
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// Puts a movie on the programme, provided the auditorium is free for the whole slot.
    /// </summary>
    /// <param name="movieId">The movie to show.</param>
    /// <param name="movieDuration">Its running time, copied from the movie as it is now.</param>
    /// <param name="auditoriumId">The auditorium to use.</param>
    /// <param name="startsAt">When the audience is admitted.</param>
    /// <param name="price">What one seat costs.</param>
    /// <param name="auditoriumSchedule">Every slot that auditorium is already committed to.</param>
    /// <param name="clock">The clock, so that "in the past" is testable.</param>
    /// <exception cref="ScreeningInThePastException">The slot has already begun.</exception>
    /// <exception cref="OverlappingScreeningException">The auditorium is busy during part of the slot.</exception>
    public static Screening Schedule(
        Guid movieId,
        TimeSpan movieDuration,
        Guid auditoriumId,
        DateTimeOffset startsAt,
        Money price,
        IEnumerable<ScheduledSlot> auditoriumSchedule,
        TimeProvider clock)
    {
        DateTimeOffset now = clock.GetUtcNow();
        TimeSlot slot = OccupationOf(startsAt, movieDuration, now);
        EnsureAuditoriumIsFree(auditoriumId, slot, auditoriumSchedule);

        var screening = new Screening(Guid.CreateVersion7(), movieId, auditoriumId, slot, price);
        screening.Raise(new ScreeningScheduledDomainEvent(
            now,
            screening.Id,
            movieId,
            auditoriumId,
            slot.StartsAt,
            slot.EndsAt,
            price.Amount,
            price.Currency));

        return screening;
    }

    /// <summary>
    /// Moves the screening or changes its price. The auditorium and the movie cannot change: that would be
    /// a different showing, and any booking already taken would silently mean something else.
    /// </summary>
    /// <param name="movieDuration">The movie's running time as it is now.</param>
    /// <param name="startsAt">The new admission time.</param>
    /// <param name="price">The new seat price.</param>
    /// <param name="auditoriumSchedule">Every slot the auditorium is committed to, this screening included.</param>
    /// <param name="clock">The clock, so that "in the past" is testable.</param>
    /// <exception cref="ScreeningCancelledException">The screening was cancelled.</exception>
    /// <exception cref="ScreeningAlreadyStartedException">The screening has already begun.</exception>
    /// <exception cref="OverlappingScreeningException">The auditorium is busy during part of the new slot.</exception>
    public void Reschedule(
        TimeSpan movieDuration,
        DateTimeOffset startsAt,
        Money price,
        IEnumerable<ScheduledSlot> auditoriumSchedule,
        TimeProvider clock)
    {
        DateTimeOffset now = clock.GetUtcNow();
        EnsureStillChangeable(now);

        TimeSlot slot = OccupationOf(startsAt, movieDuration, now);

        // Its own current slot is not a conflict with itself.
        EnsureAuditoriumIsFree(
            AuditoriumId,
            slot,
            auditoriumSchedule.Where(occupied => occupied.ScreeningId != Id));

        Slot = slot;
        Price = price;

        Raise(new ScreeningRescheduledDomainEvent(
            now, Id, slot.StartsAt, slot.EndsAt, price.Amount, price.Currency));
    }

    /// <summary>
    /// Calls the screening off, keeping the row so that bookings referring to it still resolve.
    /// </summary>
    /// <remarks>
    /// Idempotent by design: cancelling an already cancelled screening changes nothing and raises no second
    /// event, so a client that retries a timed-out request gets the same answer instead of an error.
    /// </remarks>
    /// <param name="clock">The clock, so that "already started" is testable.</param>
    /// <param name="reason">Why it was called off, if a reason was given.</param>
    /// <exception cref="ScreeningAlreadyStartedException">The screening has already begun.</exception>
    public void Cancel(TimeProvider clock, string? reason = null)
    {
        if (Status == ScreeningStatus.Cancelled)
        {
            return;
        }

        DateTimeOffset now = clock.GetUtcNow();
        EnsureHasNotStarted(now);

        Status = ScreeningStatus.Cancelled;
        CancelledAt = now;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        Raise(new ScreeningCancelledDomainEvent(now, Id, CancellationReason));
    }

    /// <summary>
    /// How long the auditorium is tied up by a film of this length starting then: the running time plus
    /// <see cref="CleaningBuffer"/>.
    /// </summary>
    /// <remarks>
    /// Public because a caller has to know the slot before it can ask which screenings might clash with it.
    /// Keeping the arithmetic here means the question asked of the database and the rule applied afterwards
    /// cannot disagree about what "occupied" means.
    /// </remarks>
    public static TimeSlot OccupationFor(DateTimeOffset startsAt, TimeSpan movieDuration) =>
        TimeSlot.Of(startsAt, movieDuration + CleaningBuffer);

    private static TimeSlot OccupationOf(DateTimeOffset startsAt, TimeSpan movieDuration, DateTimeOffset now)
    {
        if (startsAt <= now)
        {
            throw new ScreeningInThePastException(startsAt, now);
        }

        return OccupationFor(startsAt, movieDuration);
    }

    private static void EnsureAuditoriumIsFree(
        Guid auditoriumId,
        TimeSlot slot,
        IEnumerable<ScheduledSlot> auditoriumSchedule)
    {
        ScheduledSlot? conflict = auditoriumSchedule.FirstOrDefault(occupied => occupied.Slot.Overlaps(slot));

        if (conflict is not null)
        {
            throw new OverlappingScreeningException(auditoriumId, conflict.ScreeningId);
        }
    }

    private void EnsureStillChangeable(DateTimeOffset now)
    {
        if (Status == ScreeningStatus.Cancelled)
        {
            throw new ScreeningCancelledException(Id);
        }

        EnsureHasNotStarted(now);
    }

    private void EnsureHasNotStarted(DateTimeOffset now)
    {
        if (Slot.HasStartedBy(now))
        {
            throw new ScreeningAlreadyStartedException(Id, Slot.StartsAt);
        }
    }
}
