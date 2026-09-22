using System.Net;
using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.Persistence.Inbox;
using QubicaCinema.Bookings.Infrastructure.Persistence;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Bookings;

/// <summary>
/// Proves the inbox: an event Booking has already applied becomes bookable exactly once, and a redelivery
/// of the same <c>EventId</c> leaves exactly one row behind rather than a second one, or a crash.
/// </summary>
public sealed class InboxTests(CinemaFixture cinema)
{
    [Fact]
    public async Task A_screening_Catalog_announces_becomes_bookable_in_Booking()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // ScheduleAndProjectAsync already waits for exactly this; the assertion states what that wait proved.
        (_, SeatMapResponse seatMap) = await cinema.ScheduleAndProjectAsync(cancellationToken);

        seatMap.Counts.Available.ShouldBe(seatMap.Seats.Count);
    }

    [Fact]
    public async Task A_redelivered_event_leaves_exactly_one_inbox_row()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (Guid screeningId, _) = await cinema.ScheduleAndProjectAsync(cancellationToken);

        Guid eventId = await Outbox.RepublishAsync(
            cinema.Catalog, CatalogEventNames.ScreeningScheduled, screeningId, cancellationToken);

        // The fact that settles the race: once the queue is idle again, the redelivery has been acked, which
        // only happens after RabbitMqConsumerService has run it through the inbox check.
        await cinema.WaitForBookingQueueIdleAsync(cancellationToken);

        await using AsyncServiceScope scope = cinema.Bookings.Services.CreateAsyncScope();
        BookingDbContext context = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

        int rows = await context.Set<InboxMessage>().CountAsync(message => message.EventId == eventId, cancellationToken);
        rows.ShouldBe(1);
    }

    [Fact]
    public async Task Cancelling_the_screening_releases_the_seats_bookings_held_on_it()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (Guid screeningId, SeatMapResponse seatMap) = await cinema.ScheduleAndProjectAsync(cancellationToken);
        Guid seatId = seatMap.Seats[0].SeatId;

        HttpClient customer = cinema.NewCustomer();
        using HttpResponseMessage booking =
            await customer.BookSeatsAsync(screeningId, [seatId], Guid.NewGuid().ToString(), cancellationToken);
        booking.StatusCode.ShouldBe(HttpStatusCode.Created);

        HttpClient admin = cinema.Catalog.CreateClientFor(Guid.CreateVersion7(), CinemaRoles.Admin);
        using HttpResponseMessage cancellation = await admin.PostAsJsonAsync(
            $"/api/v1/screenings/{screeningId}/cancellation", new { reason = "Projector fault" }, cancellationToken);
        cancellation.StatusCode.ShouldBe(HttpStatusCode.OK);

        await Outbox.DrainAsync(cinema.Catalog, cancellationToken);

        SeatMapResponse afterwards = await customer.WaitForReleasedSeatAsync(screeningId, seatId, cancellationToken);
        afterwards.Status.ShouldBe("Cancelled");
    }
}
