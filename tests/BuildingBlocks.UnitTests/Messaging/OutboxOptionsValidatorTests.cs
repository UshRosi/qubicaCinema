using Microsoft.Extensions.Options;
using QubicaCinema.BuildingBlocks.Persistence.Outbox;

namespace QubicaCinema.BuildingBlocks.UnitTests.Messaging;

public sealed class OutboxOptionsValidatorTests
{
    private readonly OutboxOptionsValidator _validator = new();

    [Fact]
    public void Should_accept_the_defaults() =>
        _validator.Validate(null, new OutboxOptions()).Succeeded.ShouldBeTrue();

    [Fact]
    public void Should_refuse_a_claim_that_expires_before_the_next_poll()
    {
        // Otherwise the next poll claims the batch again while it is still being published, and every
        // message in it goes out twice.
        var options = new OutboxOptions
        {
            PollingInterval = TimeSpan.FromSeconds(10),
            ClaimTimeout = TimeSpan.FromSeconds(10),
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(OutboxOptions.ClaimTimeout));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Should_refuse_a_batch_without_messages(int batchSize) =>
        _validator.Validate(null, new OutboxOptions { BatchSize = batchSize }).Failed.ShouldBeTrue();

    [Fact]
    public void Should_refuse_a_polling_interval_of_zero() =>
        _validator.Validate(null, new OutboxOptions { PollingInterval = TimeSpan.Zero }).Failed.ShouldBeTrue();

    [Fact]
    public void Should_report_every_problem_at_once()
    {
        var options = new OutboxOptions
        {
            BatchSize = 0,
            PollingInterval = TimeSpan.Zero,
            ClaimTimeout = TimeSpan.Zero,
        };

        _validator.Validate(null, options).Failures!.Count().ShouldBe(3);
    }
}
