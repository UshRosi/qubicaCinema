using Microsoft.Extensions.Time.Testing;
using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.Screenings;
using QubicaCinema.Catalog.Domain.ValueObjects;

namespace QubicaCinema.Catalog.UnitTests.Screenings;

public sealed class ScreeningTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tonight = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan FeatureLength = TimeSpan.FromMinutes(120);

    private readonly FakeTimeProvider _clock = new(Now);
    private readonly Guid _movieId = Guid.CreateVersion7();
    private readonly Guid _auditoriumId = Guid.CreateVersion7();

    [Fact]
    public void Should_occupy_the_auditorium_for_the_film_plus_the_cleaning_buffer()
    {
        Screening screening = Schedule(Tonight);

        screening.Slot.StartsAt.ShouldBe(Tonight);
        screening.Slot.EndsAt.ShouldBe(Tonight + FeatureLength + Screening.CleaningBuffer);
        screening.Status.ShouldBe(ScreeningStatus.Scheduled);
    }

    [Fact]
    public void Should_record_that_it_was_scheduled()
    {
        Screening screening = Schedule(Tonight);

        ScreeningScheduledDomainEvent raised = screening.DomainEvents.OfType<ScreeningScheduledDomainEvent>().ShouldHaveSingleItem();
        raised.ScreeningId.ShouldBe(screening.Id);
        raised.MovieId.ShouldBe(_movieId);
        raised.AuditoriumId.ShouldBe(_auditoriumId);
        raised.OccurredAt.ShouldBe(Now);
        raised.PriceAmount.ShouldBe(9.5m);
    }

    [Fact]
    public void Should_reject_a_screening_that_starts_in_the_past()
    {
        Should.Throw<ScreeningInThePastException>(() => Schedule(Now.AddMinutes(-1)));
        Should.Throw<ScreeningInThePastException>(() => Schedule(Now));
    }

    [Fact]
    public void Should_reject_a_screening_overlapping_another_in_the_same_auditorium()
    {
        Screening existing = Schedule(Tonight);

        var exception = Should.Throw<OverlappingScreeningException>(
            () => Schedule(Tonight.AddMinutes(60), Occupied(existing)));

        exception.Extensions["conflictingScreeningId"].ShouldBe(existing.Id);
        exception.Extensions["auditoriumId"].ShouldBe(_auditoriumId);
    }

    [Fact]
    public void Should_reject_a_screening_starting_inside_the_cleaning_buffer()
    {
        Screening existing = Schedule(Tonight);

        // The film is over, but the room is not free yet.
        Should.Throw<OverlappingScreeningException>(
            () => Schedule(Tonight + FeatureLength + Screening.CleaningBuffer - TimeSpan.FromMinutes(1), Occupied(existing)));
    }

    [Fact]
    public void Should_accept_a_screening_starting_the_moment_the_room_is_free()
    {
        Screening existing = Schedule(Tonight);

        Screening next = Schedule(Tonight + FeatureLength + Screening.CleaningBuffer, Occupied(existing));

        next.Slot.StartsAt.ShouldBe(Tonight + FeatureLength + Screening.CleaningBuffer);
    }

    [Fact]
    public void Should_ignore_the_schedule_of_other_auditoriums()
    {
        // The caller passes only the slots of the auditorium in question; an empty schedule means it is free.
        Schedule(Tonight, []).ShouldNotBeNull();
    }

    [Fact]
    public void Should_move_a_screening_and_record_it()
    {
        Screening screening = Schedule(Tonight);
        screening.ClearDomainEvents();

        screening.Reschedule(FeatureLength, Tonight.AddDays(1), Money.Of(11m, "EUR"), Occupied(screening), _clock);

        screening.Slot.StartsAt.ShouldBe(Tonight.AddDays(1));
        screening.Price.Amount.ShouldBe(11m);
        screening.DomainEvents.OfType<ScreeningRescheduledDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Should_not_treat_a_screening_as_a_conflict_with_itself()
    {
        Screening screening = Schedule(Tonight);

        // Same slot, same price: a no-op edit must not collide with the row it is editing.
        Should.NotThrow(() => screening.Reschedule(FeatureLength, Tonight, Money.Of(9.5m, "EUR"), Occupied(screening), _clock));
    }

    [Fact]
    public void Should_refuse_to_move_a_screening_that_has_started()
    {
        Screening screening = Schedule(Tonight);
        _clock.SetUtcNow(Tonight);

        Should.Throw<ScreeningAlreadyStartedException>(
            () => screening.Reschedule(FeatureLength, Tonight.AddDays(1), Money.Of(11m, "EUR"), Occupied(screening), _clock));
    }

    [Fact]
    public void Should_refuse_to_move_a_cancelled_screening()
    {
        Screening screening = Schedule(Tonight);
        screening.Cancel(_clock);

        Should.Throw<ScreeningCancelledException>(
            () => screening.Reschedule(FeatureLength, Tonight.AddDays(1), Money.Of(11m, "EUR"), Occupied(screening), _clock));
    }

    [Fact]
    public void Should_cancel_a_screening_and_keep_it()
    {
        Screening screening = Schedule(Tonight);
        screening.ClearDomainEvents();

        screening.Cancel(_clock, "  projector failure  ");

        screening.Status.ShouldBe(ScreeningStatus.Cancelled);
        screening.CancelledAt.ShouldBe(Now);
        screening.CancellationReason.ShouldBe("projector failure");
        screening.DomainEvents.OfType<ScreeningCancelledDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Should_be_idempotent_when_cancelled_twice()
    {
        Screening screening = Schedule(Tonight);
        screening.Cancel(_clock, "projector failure");
        screening.ClearDomainEvents();

        _clock.SetUtcNow(Now.AddHours(1));
        screening.Cancel(_clock, "a different reason");

        screening.CancelledAt.ShouldBe(Now);
        screening.CancellationReason.ShouldBe("projector failure");
        screening.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Should_refuse_to_cancel_a_screening_that_has_started()
    {
        Screening screening = Schedule(Tonight);
        _clock.SetUtcNow(Tonight);

        Should.Throw<ScreeningAlreadyStartedException>(() => screening.Cancel(_clock));
    }

    private Screening Schedule(DateTimeOffset startsAt, IEnumerable<ScheduledSlot>? auditoriumSchedule = null) =>
        Screening.Schedule(
            _movieId,
            FeatureLength,
            _auditoriumId,
            startsAt,
            Money.Of(9.5m, "EUR"),
            auditoriumSchedule ?? [],
            _clock);

    private static ScheduledSlot[] Occupied(params Screening[] screenings) =>
        [.. screenings.Select(screening => new ScheduledSlot(screening.Id, screening.Slot))];
}
