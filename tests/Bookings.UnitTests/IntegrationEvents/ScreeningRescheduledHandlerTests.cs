using NSubstitute;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Application.IntegrationEvents;
using QubicaCinema.Bookings.Domain.Exceptions;
using QubicaCinema.Bookings.Domain.Screenings;
using QubicaCinema.Bookings.UnitTests.Fixtures;

namespace QubicaCinema.Bookings.UnitTests.IntegrationEvents;

public sealed class ScreeningRescheduledHandlerTests
{
    private static readonly DateTimeOffset Happened = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IScreeningRepository _screenings = Substitute.For<IScreeningRepository>();
    private readonly ScreeningRescheduledHandler _handler;

    public ScreeningRescheduledHandlerTests() => _handler = new ScreeningRescheduledHandler(_screenings);

    [Fact]
    public async Task Should_apply_the_new_time_and_price()
    {
        Screening screening = new TestAuditorium(1, 1).Screening(Happened.AddDays(1), price: 9.50m);
        _screenings.FindAsync(screening.Id, Arg.Any<CancellationToken>()).Returns(screening);

        await _handler.HandleAsync(
            new ScreeningRescheduled(screening.Id, Happened.AddDays(2), Happened.AddDays(2).AddHours(3), 12m, "EUR")
            {
                OccurredAt = Happened,
            },
            CancellationToken.None);

        screening.StartsAt.ShouldBe(Happened.AddDays(2));
        screening.Price.ShouldBe(Money.Of(12m, "EUR"));
    }

    [Fact]
    public async Task Should_fail_for_an_unknown_screening_so_the_broker_redelivers_it()
    {
        var unknown = new ScreeningRescheduled(Guid.CreateVersion7(), Happened, Happened, 12m, "EUR") { OccurredAt = Happened };
        _screenings.FindAsync(unknown.ScreeningId, Arg.Any<CancellationToken>()).Returns((Screening?)null);

        await Should.ThrowAsync<ScreeningNotFoundException>(() => _handler.HandleAsync(unknown, CancellationToken.None));
    }
}
