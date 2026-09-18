using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Application.Screenings.ScheduleScreening;
using QubicaCinema.Catalog.Domain.Auditoriums;
using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.Movies;
using QubicaCinema.Catalog.Domain.Screenings;
using QubicaCinema.Catalog.Domain.ValueObjects;

namespace QubicaCinema.Catalog.UnitTests.Handlers;

/// <summary>
/// The handler's own responsibilities: loading what the aggregate needs, asking the database the right
/// question, and committing once. The scheduling rule itself is tested against the aggregate directly.
/// </summary>
public sealed class ScheduleScreeningHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tonight = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    private readonly IMovieRepository _movies = Substitute.For<IMovieRepository>();
    private readonly IAuditoriumRepository _auditoriums = Substitute.For<IAuditoriumRepository>();
    private readonly IScreeningRepository _screenings = Substitute.For<IScreeningRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = new(Now);

    private readonly Movie _movie = Movie.Create(
        "Dune", "Spice.", TimeSpan.FromMinutes(120), MovieGenre.ScienceFiction, AgeRating.Teen);

    private readonly Auditorium _auditorium = Auditorium.Create("Sala Rossa", 4, 6);

    [Fact]
    public async Task Should_schedule_the_screening_and_commit_once()
    {
        GivenTheMovieAndAuditoriumExist();

        Guid id = await CreateHandler().HandleAsync(Command(), TestContext.Current.CancellationToken);

        id.ShouldNotBe(Guid.Empty);
        _screenings.Received(1).Add(Arg.Is<Screening>(screening =>
            screening.MovieId == _movie.Id
            && screening.AuditoriumId == _auditorium.Id
            && screening.Slot.StartsAt == Tonight));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ask_the_database_only_about_the_slot_the_screening_would_occupy()
    {
        GivenTheMovieAndAuditoriumExist();

        await CreateHandler().HandleAsync(Command(), TestContext.Current.CancellationToken);

        // The window is the film plus the cleaning buffer, not an open-ended read of the programme.
        await _screenings.Received(1).GetScheduleAsync(
            _auditorium.Id,
            Tonight,
            Tonight + _movie.Duration + Screening.CleaningBuffer,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_report_an_unknown_movie_as_not_found()
    {
        _movies.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Movie?)null);

        await Should.ThrowAsync<MovieNotFoundException>(
            CreateHandler().HandleAsync(Command(), TestContext.Current.CancellationToken));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_report_an_unknown_auditorium_as_not_found()
    {
        _movies.FindAsync(_movie.Id, Arg.Any<CancellationToken>()).Returns(_movie);
        _auditoriums.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Should.ThrowAsync<AuditoriumNotFoundException>(
            CreateHandler().HandleAsync(Command(), TestContext.Current.CancellationToken));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_not_commit_when_the_auditorium_is_already_busy()
    {
        GivenTheMovieAndAuditoriumExist();

        var occupied = new ScheduledSlot(
            Guid.CreateVersion7(),
            TimeSlot.Of(Tonight.AddMinutes(-30), TimeSpan.FromHours(2)));

        _screenings.GetScheduleAsync(
                Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([occupied]);

        await Should.ThrowAsync<OverlappingScreeningException>(
            CreateHandler().HandleAsync(Command(), TestContext.Current.CancellationToken));

        _screenings.DidNotReceive().Add(Arg.Any<Screening>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void GivenTheMovieAndAuditoriumExist()
    {
        _movies.FindAsync(_movie.Id, Arg.Any<CancellationToken>()).Returns(_movie);
        _auditoriums.ExistsAsync(_auditorium.Id, Arg.Any<CancellationToken>()).Returns(true);
        _screenings.GetScheduleAsync(
                Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private ScheduleScreeningCommand Command() =>
        new(_movie.Id, _auditorium.Id, Tonight, 9.5m, "EUR");

    private ScheduleScreeningHandler CreateHandler() =>
        new(_movies, _auditoriums, _screenings, _unitOfWork, _clock);
}
