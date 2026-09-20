using Microsoft.Extensions.Time.Testing;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.Catalog.Domain.Auditoriums;
using QubicaCinema.Catalog.Domain.Movies;
using QubicaCinema.Catalog.Domain.Screenings;
using QubicaCinema.Catalog.Infrastructure.Messaging;

namespace QubicaCinema.Catalog.UnitTests.Messaging;

/// <summary>
/// What Catalog says to the other services is a contract, so the translation from its own domain events is
/// pinned down field by field.
/// </summary>
public sealed class ScreeningIntegrationEventsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tonight = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Now);
    private readonly Movie _movie = Movie.Create(
        "Dune: Part Two", "Paul Atreides unites with the Fremen.", TimeSpan.FromMinutes(166), MovieGenre.Adventure, AgeRating.Teen);
    private readonly Auditorium _room = Auditorium.Create("Sala Rossa", rowCount: 2, seatsPerRow: 3);

    [Fact]
    public void Should_announce_a_new_screening_with_everything_a_consumer_needs()
    {
        Screening screening = Schedule();
        var raised = screening.DomainEvents.OfType<ScreeningScheduledDomainEvent>().ShouldHaveSingleItem();

        ScreeningScheduled announced = ScreeningIntegrationEvents.From(raised, _movie, _room);

        announced.ScreeningId.ShouldBe(screening.Id);
        announced.MovieId.ShouldBe(_movie.Id);
        announced.MovieTitle.ShouldBe("Dune: Part Two");
        announced.AuditoriumId.ShouldBe(_room.Id);
        announced.AuditoriumName.ShouldBe("Sala Rossa");
        announced.StartsAt.ShouldBe(Tonight);
        announced.EndsAt.ShouldBe(screening.Slot.EndsAt);
        announced.PriceAmount.ShouldBe(9.5m);
        announced.PriceCurrency.ShouldBe("EUR");
    }

    [Fact]
    public void Should_carry_the_whole_seat_map_in_reading_order()
    {
        Screening screening = Schedule();
        var raised = screening.DomainEvents.OfType<ScreeningScheduledDomainEvent>().Single();

        ScreeningScheduled announced = ScreeningIntegrationEvents.From(raised, _movie, _room);

        announced.Seats.Select(seat => $"{seat.Row}{seat.Number}").ShouldBe(["A1", "A2", "A3", "B1", "B2", "B3"]);
        announced.Seats.Select(seat => seat.SeatId).ShouldBe(
            _room.Seats.OrderBy(seat => seat.Position.Row, StringComparer.Ordinal).ThenBy(seat => seat.Position.Number).Select(seat => seat.Id));
    }

    [Fact]
    public void Should_stamp_the_event_with_the_time_the_model_decided_it()
    {
        Screening screening = Schedule();
        var raised = screening.DomainEvents.OfType<ScreeningScheduledDomainEvent>().Single();

        _clock.Advance(TimeSpan.FromHours(3));

        ScreeningIntegrationEvents.From(raised, _movie, _room).OccurredAt.ShouldBe(Now);
    }

    [Fact]
    public void Should_announce_a_new_time_and_price()
    {
        Screening screening = Schedule();
        screening.Reschedule(TimeSpan.FromMinutes(166), Tonight.AddHours(1), Money.Of(12m, "EUR"), [], _clock);
        var raised = screening.DomainEvents.OfType<ScreeningRescheduledDomainEvent>().ShouldHaveSingleItem();

        ScreeningRescheduled announced = ScreeningIntegrationEvents.From(raised);

        announced.ScreeningId.ShouldBe(screening.Id);
        announced.StartsAt.ShouldBe(Tonight.AddHours(1));
        announced.EndsAt.ShouldBe(screening.Slot.EndsAt);
        announced.PriceAmount.ShouldBe(12m);
        announced.OccurredAt.ShouldBe(Now);
    }

    [Fact]
    public void Should_announce_a_cancellation_with_its_reason()
    {
        Screening screening = Schedule();
        screening.Cancel(_clock, "Projector failure");
        var raised = screening.DomainEvents.OfType<ScreeningCancelledDomainEvent>().ShouldHaveSingleItem();

        ScreeningCancelled announced = ScreeningIntegrationEvents.From(raised);

        announced.ScreeningId.ShouldBe(screening.Id);
        announced.Reason.ShouldBe("Projector failure");
    }

    [Fact]
    public void Should_announce_a_cancellation_without_a_reason_when_none_was_given()
    {
        Screening screening = Schedule();
        screening.Cancel(_clock);

        ScreeningIntegrationEvents
            .From(screening.DomainEvents.OfType<ScreeningCancelledDomainEvent>().Single())
            .Reason.ShouldBeNull();
    }

    private Screening Schedule() =>
        Screening.Schedule(
            _movie.Id, _movie.Duration, _room.Id, Tonight, Money.Of(9.5m, "EUR"), [], _clock);
}
