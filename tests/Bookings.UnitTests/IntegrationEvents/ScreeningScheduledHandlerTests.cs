using NSubstitute;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Application.IntegrationEvents;
using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.UnitTests.IntegrationEvents;

public sealed class ScreeningScheduledHandlerTests
{
    private static readonly DateTimeOffset Happened = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IScreeningRepository _screenings = Substitute.For<IScreeningRepository>();
    private readonly ISeatRepository _seats = Substitute.For<ISeatRepository>();
    private readonly ScreeningScheduledHandler _handler;
    private readonly ScreeningScheduled _event;

    public ScreeningScheduledHandlerTests()
    {
        _handler = new ScreeningScheduledHandler(_screenings, _seats);
        _event = new ScreeningScheduled(
            ScreeningId: Guid.CreateVersion7(),
            MovieId: Guid.CreateVersion7(),
            MovieTitle: "Heat",
            AuditoriumId: Guid.CreateVersion7(),
            AuditoriumName: "Sala Piccola",
            StartsAt: Happened.AddDays(1),
            EndsAt: Happened.AddDays(1).AddHours(3),
            PriceAmount: 7.50m,
            PriceCurrency: "EUR",
            Seats:
            [
                new ScreeningSeat(Guid.CreateVersion7(), "A", 1),
                new ScreeningSeat(Guid.CreateVersion7(), "A", 2),
                new ScreeningSeat(Guid.CreateVersion7(), "B", 1),
            ])
        {
            OccurredAt = Happened,
        };

        _screenings.FindAsync(_event.ScreeningId, Arg.Any<CancellationToken>()).Returns((Screening?)null);
        _seats.GetKnownIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());
    }

    [Fact]
    public async Task Should_add_the_screening_with_catalogs_own_ids()
    {
        await _handler.HandleAsync(_event, CancellationToken.None);

        _screenings.Received(1).Add(Arg.Is<Screening>(screening =>
            screening.Id == _event.ScreeningId
            && screening.AuditoriumId == _event.AuditoriumId
            && screening.AuditoriumName == "Sala Piccola"
            && screening.MovieTitle == "Heat"
            && screening.StartsAt == _event.StartsAt
            && screening.Price.Amount == 7.50m
            && screening.Status == ScreeningStatus.Scheduled));
    }

    [Fact]
    public async Task Should_add_every_seat_of_a_new_auditorium()
    {
        IEnumerable<Seat>? added = null;
        _seats.When(seats => seats.AddRange(Arg.Any<IEnumerable<Seat>>()))
            .Do(call => added = [.. call.Arg<IEnumerable<Seat>>()]);

        await _handler.HandleAsync(_event, CancellationToken.None);

        added.ShouldNotBeNull();
        added.Select(seat => seat.Id).ShouldBe(_event.Seats.Select(seat => seat.SeatId));
        added.ShouldAllBe(seat => seat.AuditoriumId == _event.AuditoriumId);
        added.Select(seat => seat.ToString()).ShouldBe(["A1", "A2", "B1"]);
    }

    [Fact]
    public async Task Should_not_add_seats_it_already_knows_from_another_screening_in_the_same_room()
    {
        Guid[] known = [_event.Seats[0].SeatId, _event.Seats[1].SeatId];
        _seats.GetKnownIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(known.ToHashSet());
        IEnumerable<Seat>? added = null;
        _seats.When(seats => seats.AddRange(Arg.Any<IEnumerable<Seat>>()))
            .Do(call => added = [.. call.Arg<IEnumerable<Seat>>()]);

        await _handler.HandleAsync(_event, CancellationToken.None);

        added.ShouldNotBeNull().Select(seat => seat.Id).ShouldBe([_event.Seats[2].SeatId]);
    }

    [Fact]
    public async Task Should_do_nothing_when_the_screening_is_already_known()
    {
        _screenings.FindAsync(_event.ScreeningId, Arg.Any<CancellationToken>())
            .Returns(Screening.Create(
                _event.ScreeningId, _event.AuditoriumId, "Sala Piccola", "Heat", _event.StartsAt,
                BuildingBlocks.Domain.ValueObjects.Money.Of(7.50m, "EUR")));

        await _handler.HandleAsync(_event, CancellationToken.None);

        _screenings.DidNotReceive().Add(Arg.Any<Screening>());
        _seats.DidNotReceive().AddRange(Arg.Any<IEnumerable<Seat>>());
    }
}
