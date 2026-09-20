using System.Diagnostics;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.Persistence.Outbox;

namespace QubicaCinema.BuildingBlocks.UnitTests.Messaging;

public sealed class OutboxMessageTests
{
    private static readonly DateTimeOffset Happened = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ScreeningCancelled _event = new(Guid.CreateVersion7(), "Projector failure") { OccurredAt = Happened };

    [Fact]
    public void Should_store_the_wire_name_and_the_events_own_id()
    {
        var message = OutboxMessage.From(_event, activity: null);

        message.Id.ShouldBe(_event.EventId);
        message.EventName.ShouldBe(ScreeningCancelled.Manifest);
        message.OccurredAt.ShouldBe(Happened);
        message.ProcessedAt.ShouldBeNull();
        message.Attempts.ShouldBe(0);
    }

    [Fact]
    public void Should_store_the_payload_as_it_will_be_published()
    {
        var message = OutboxMessage.From(_event, activity: null);

        message.Payload.ShouldContain(_event.ScreeningId.ToString());
        message.Payload.ShouldContain("Projector failure");
    }

    [Fact]
    public void Should_hand_the_bus_the_same_message_it_stored()
    {
        var message = OutboxMessage.From(_event, activity: null);

        var outgoing = message.ToOutgoing();

        outgoing.EventId.ShouldBe(message.Id);
        outgoing.EventName.ShouldBe(message.EventName);
        outgoing.Payload.ShouldBe(message.Payload);
    }

    [Fact]
    public void Should_remember_the_trace_it_was_raised_in()
    {
        using var source = new ActivitySource("test.outbox");
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate.Name == "test.outbox",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using Activity activity = source.StartActivity("request")!;

        var message = OutboxMessage.From(_event, activity);

        message.TraceParent.ShouldBe(activity.Id);
        ActivityContext.TryParse(message.TraceParent, message.TraceState, out ActivityContext restored).ShouldBeTrue();
        restored.TraceId.ShouldBe(activity.TraceId);
    }

    [Fact]
    public void Should_have_no_trace_when_nothing_was_in_progress() =>
        OutboxMessage.From(_event, activity: null).TraceParent.ShouldBeNull();

    [Fact]
    public void Should_count_a_failure_and_give_the_row_back()
    {
        var message = OutboxMessage.From(_event, activity: null);

        message.RecordFailure("broker down");
        message.RecordFailure("broker still down");

        message.Attempts.ShouldBe(2);
        message.LastError.ShouldBe("broker still down");
        message.ClaimedUntil.ShouldBeNull();
        message.ProcessedAt.ShouldBeNull();
    }

    [Fact]
    public void Should_keep_an_error_within_the_column()
    {
        var message = OutboxMessage.From(_event, activity: null);

        message.RecordFailure(new string('x', OutboxMessage.MaxErrorLength + 500));

        message.LastError!.Length.ShouldBe(OutboxMessage.MaxErrorLength);
    }

    [Fact]
    public void Should_be_marked_published_and_forget_an_earlier_failure()
    {
        var message = OutboxMessage.From(_event, activity: null);
        message.RecordFailure("broker down");

        message.MarkPublished(Happened.AddSeconds(5));

        message.ProcessedAt.ShouldBe(Happened.AddSeconds(5));
        message.LastError.ShouldBeNull();
    }

    [Fact]
    public void Should_release_an_untried_row_without_counting_an_attempt()
    {
        var message = OutboxMessage.From(_event, activity: null);

        message.Release();

        message.Attempts.ShouldBe(0);
        message.ClaimedUntil.ShouldBeNull();
    }
}
