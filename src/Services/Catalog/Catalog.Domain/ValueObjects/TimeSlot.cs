using QubicaCinema.Catalog.Domain.Exceptions;

namespace QubicaCinema.Catalog.Domain.ValueObjects;

/// <summary>
/// A half-open interval in time, <c>[StartsAt, EndsAt)</c>.
/// </summary>
/// <remarks>
/// Half-open is the whole point: a slot ending at 20:00 and one starting at 20:00 do not overlap, so
/// back-to-back screenings are legal while a one-minute encroachment is not. Writing that comparison inline
/// at every call site is how off-by-one bugs get in, so the rule lives here and is unit tested on its own.
/// </remarks>
public sealed record TimeSlot
{
    private TimeSlot(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    /// <summary>When the slot begins. Inclusive.</summary>
    public DateTimeOffset StartsAt { get; }

    /// <summary>When the slot ends. Exclusive.</summary>
    public DateTimeOffset EndsAt { get; }

    /// <summary>How long the slot lasts.</summary>
    public TimeSpan Duration => EndsAt - StartsAt;

    /// <summary>Creates a slot from its two ends.</summary>
    /// <exception cref="InvalidTimeSlotException">The end does not follow the start.</exception>
    public static TimeSlot Of(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (endsAt <= startsAt)
        {
            throw new InvalidTimeSlotException(
                $"A time slot must end after it starts, but {endsAt:u} does not follow {startsAt:u}.");
        }

        return new TimeSlot(startsAt, endsAt);
    }

    /// <summary>Creates a slot from its start and how long it lasts.</summary>
    /// <exception cref="InvalidTimeSlotException">The duration is zero or negative.</exception>
    public static TimeSlot Of(DateTimeOffset startsAt, TimeSpan duration) => Of(startsAt, startsAt + duration);

    /// <summary>Whether this slot and another share any instant at all.</summary>
    public bool Overlaps(TimeSlot other) => StartsAt < other.EndsAt && other.StartsAt < EndsAt;

    /// <summary>Whether the given instant falls inside the slot.</summary>
    public bool Contains(DateTimeOffset instant) => instant >= StartsAt && instant < EndsAt;

    /// <summary>Whether the slot has already begun at the given instant.</summary>
    public bool HasStartedBy(DateTimeOffset instant) => instant >= StartsAt;

    /// <summary>Renders both ends, for logs and traces.</summary>
    public override string ToString() => $"{StartsAt:u} – {EndsAt:u}";
}
