using System.Text.Json;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.EventBus;

namespace QubicaCinema.BuildingBlocks.UnitTests.Contracts;

/// <summary>
/// The event contracts are frozen: a service deployed tomorrow must read what one deployed last month wrote.
/// These tests are the tripwire for the extend-only rules.
/// </summary>
public sealed class IntegrationEventContractTests
{
    private static readonly DateTimeOffset Happened = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly ScreeningScheduled Scheduled = new(
        ScreeningId: Guid.CreateVersion7(),
        MovieId: Guid.CreateVersion7(),
        MovieTitle: "Dune: Part Two",
        AuditoriumId: Guid.CreateVersion7(),
        AuditoriumName: "Sala Rossa",
        StartsAt: Happened.AddDays(1),
        EndsAt: Happened.AddDays(1).AddHours(3),
        PriceAmount: 11.50m,
        PriceCurrency: "EUR",
        Seats: [new ScreeningSeat(Guid.CreateVersion7(), "A", 1), new ScreeningSeat(Guid.CreateVersion7(), "A", 2)])
    {
        OccurredAt = Happened,
    };

    [Theory]
    [InlineData("catalog.screening-scheduled.v1")]
    [InlineData("catalog.screening-rescheduled.v1")]
    [InlineData("catalog.screening-cancelled.v1")]
    public void Should_keep_the_wire_names_of_published_events(string published)
    {
        // A name that changes here orphans every stored outbox row and every message already in flight.
        string[] names = [ScreeningScheduled.Manifest, ScreeningRescheduled.Manifest, ScreeningCancelled.Manifest];

        names.ShouldContain(published);
    }

    [Fact]
    public void Should_name_an_event_by_its_manifest_never_by_its_clr_type()
    {
        Scheduled.EventName.ShouldBe(ScreeningScheduled.Manifest);
        Scheduled.EventName.ShouldNotContain(nameof(ScreeningScheduled));
        Scheduled.EventName.ShouldNotContain("QubicaCinema");
    }

    [Fact]
    public void Should_round_trip_an_event_without_losing_a_field()
    {
        string json = IntegrationEventSerializer.Serialize(Scheduled);

        ScreeningScheduled read = IntegrationEventSerializer.Deserialize<ScreeningScheduled>(json);

        // A record compares a list by reference, so the seats are checked on their own below.
        (read with { Seats = Scheduled.Seats }).ShouldBe(Scheduled);
    }

    [Fact]
    public void Should_carry_the_seat_map_in_the_payload()
    {
        ScreeningScheduled read = IntegrationEventSerializer.Deserialize<ScreeningScheduled>(
            IntegrationEventSerializer.Serialize(Scheduled));

        read.Seats.Select(seat => (seat.SeatId, seat.Row, seat.Number))
            .ShouldBe(Scheduled.Seats.Select(seat => (seat.SeatId, seat.Row, seat.Number)));
    }

    [Fact]
    public void Should_keep_the_wire_name_out_of_the_payload()
    {
        string json = IntegrationEventSerializer.Serialize(new ScreeningCancelled(Guid.CreateVersion7()) { OccurredAt = Happened });

        json.ShouldNotContain("eventName", Case.Insensitive);
    }

    [Fact]
    public void Should_ignore_a_property_it_does_not_know()
    {
        // A newer publisher added a field; an older consumer must keep working.
        var screeningId = Guid.CreateVersion7();
        string json = $$"""
            {"eventId":"{{Guid.CreateVersion7()}}","occurredAt":"2026-10-01T12:00:00+00:00",
             "screeningId":"{{screeningId}}","reason":"Projector failure","refundPolicy":"full"}
            """;

        ScreeningCancelled read = IntegrationEventSerializer.Deserialize<ScreeningCancelled>(json);

        read.ScreeningId.ShouldBe(screeningId);
        read.Reason.ShouldBe("Projector failure");
    }

    [Fact]
    public void Should_default_an_optional_field_an_older_publisher_leaves_out()
    {
        var screeningId = Guid.CreateVersion7();
        string json = $$"""{"eventId":"{{Guid.CreateVersion7()}}","occurredAt":"2026-10-01T12:00:00+00:00","screeningId":"{{screeningId}}"}""";

        ScreeningCancelled read = IntegrationEventSerializer.Deserialize<ScreeningCancelled>(json);

        read.Reason.ShouldBeNull();
    }

    [Fact]
    public void Should_refuse_a_payload_that_is_not_json() =>
        Should.Throw<JsonException>(() => IntegrationEventSerializer.Deserialize<ScreeningCancelled>("not json"));

    [Fact]
    public void Should_give_each_occurrence_its_own_id()
    {
        var first = new ScreeningCancelled(Guid.CreateVersion7()) { OccurredAt = Happened };
        var second = new ScreeningCancelled(first.ScreeningId) { OccurredAt = Happened };

        first.EventId.ShouldNotBe(second.EventId);
    }
}
