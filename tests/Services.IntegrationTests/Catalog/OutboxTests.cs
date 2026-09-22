using System.Net;
using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.Persistence.Outbox;
using QubicaCinema.Catalog.Infrastructure.Persistence;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Catalog;

/// <summary>
/// Proves that Catalog's outbox row is written in the same transaction as the screening it describes, and
/// only leaves the database once something pumps it — the guarantee no unit test can reach.
/// </summary>
public sealed class OutboxTests(CinemaFixture cinema)
{
    [Fact]
    public async Task Should_write_the_screening_and_its_outbox_row_in_one_transaction()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient admin = cinema.Catalog.CreateClientFor(Guid.CreateVersion7(), CinemaRoles.Admin);

        Guid auditoriumId = await admin.CreateAuditoriumAsync(
            $"Outbox room {Guid.NewGuid():N}", cancellationToken: cancellationToken);
        Guid movieId = await admin.CreateMovieAsync($"Outbox film {Guid.NewGuid():N}", cancellationToken: cancellationToken);

        using HttpResponseMessage schedule = await admin.ScheduleScreeningAsync(
            movieId, auditoriumId, cinema.Clock.GetUtcNow().AddHours(2), cancellationToken: cancellationToken);
        schedule.StatusCode.ShouldBe(HttpStatusCode.Created);
        var screeningId = Guid.Parse(schedule.Headers.Location!.OriginalString.Split('/')[^1]);

        OutboxMessage row = await OutboxRowForAsync(screeningId, cancellationToken);

        row.EventName.ShouldBe(CatalogEventNames.ScreeningScheduled);
        row.ProcessedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Should_write_no_outbox_row_when_scheduling_is_rejected()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient admin = cinema.Catalog.CreateClientFor(Guid.CreateVersion7(), CinemaRoles.Admin);

        Guid auditoriumId = await admin.CreateAuditoriumAsync(
            $"Overlap room {Guid.NewGuid():N}", cancellationToken: cancellationToken);
        Guid movieId = await admin.CreateMovieAsync($"Overlap film {Guid.NewGuid():N}", cancellationToken: cancellationToken);
        DateTimeOffset startsAt = cinema.Clock.GetUtcNow().AddHours(3);

        using HttpResponseMessage first = await admin.ScheduleScreeningAsync(
            movieId, auditoriumId, startsAt, cancellationToken: cancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        int before = await OutboxCountAsync(cancellationToken);

        // The exact same slot, in the same room: the model must refuse it before anything is staged.
        using HttpResponseMessage second = await admin.ScheduleScreeningAsync(
            movieId, auditoriumId, startsAt, cancellationToken: cancellationToken);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await OutboxCountAsync(cancellationToken)).ShouldBe(before);
    }

    [Fact]
    public async Task Should_publish_the_outbox_row_once_it_is_pumped()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient admin = cinema.Catalog.CreateClientFor(Guid.CreateVersion7(), CinemaRoles.Admin);

        Guid auditoriumId = await admin.CreateAuditoriumAsync(
            $"Pump room {Guid.NewGuid():N}", cancellationToken: cancellationToken);
        Guid movieId = await admin.CreateMovieAsync($"Pump film {Guid.NewGuid():N}", cancellationToken: cancellationToken);

        using HttpResponseMessage schedule = await admin.ScheduleScreeningAsync(
            movieId, auditoriumId, cinema.Clock.GetUtcNow().AddHours(4), cancellationToken: cancellationToken);
        var screeningId = Guid.Parse(schedule.Headers.Location!.OriginalString.Split('/')[^1]);

        await Outbox.DrainAsync(cinema.Catalog, cancellationToken);

        OutboxMessage row = await OutboxRowForAsync(screeningId, cancellationToken);
        row.ProcessedAt.ShouldNotBeNull();
    }

    private async Task<OutboxMessage> OutboxRowForAsync(Guid screeningId, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = cinema.Catalog.Services.CreateAsyncScope();
        CatalogDbContext context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        return await context.Set<OutboxMessage>()
            .Where(message => message.EventName == CatalogEventNames.ScreeningScheduled)
            .Where(message => EF.Functions.Like(message.Payload, $"%{screeningId}%"))
            .SingleAsync(cancellationToken);
    }

    private async Task<int> OutboxCountAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = cinema.Catalog.Services.CreateAsyncScope();
        CatalogDbContext context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        return await context.Set<OutboxMessage>().CountAsync(
            message => message.EventName == CatalogEventNames.ScreeningScheduled, cancellationToken);
    }
}
