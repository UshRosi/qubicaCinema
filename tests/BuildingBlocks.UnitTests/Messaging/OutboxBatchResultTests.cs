using QubicaCinema.BuildingBlocks.Persistence.Outbox;

namespace QubicaCinema.BuildingBlocks.UnitTests.Messaging;

public sealed class OutboxBatchResultTests
{
    [Fact]
    public void Should_go_round_again_after_a_full_batch_that_all_went_out() =>
        new OutboxBatchResult(BatchSize: 50, Published: 50, Failed: false).MoreWaiting.ShouldBeTrue();

    [Fact]
    public void Should_sleep_after_a_partial_batch() =>
        new OutboxBatchResult(BatchSize: 50, Published: 3, Failed: false).MoreWaiting.ShouldBeFalse();

    [Fact]
    public void Should_not_hammer_a_broker_that_just_refused_a_message() =>
        new OutboxBatchResult(BatchSize: 50, Published: 50, Failed: true).MoreWaiting.ShouldBeFalse();
}
