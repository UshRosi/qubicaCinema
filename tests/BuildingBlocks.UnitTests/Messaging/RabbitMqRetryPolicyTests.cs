using System.Text.Json;
using QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

namespace QubicaCinema.BuildingBlocks.UnitTests.Messaging;

public sealed class RabbitMqRetryPolicyTests
{
    private static readonly TimeSpan[] Delays = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)];

    private readonly RabbitMqRetryPolicy _policy = new(Delays);

    [Fact]
    public void Should_retry_a_first_transient_failure_after_the_shortest_delay()
    {
        RetryDecision decision = _policy.Decide(retriesSoFar: 0, new TimeoutException());

        decision.GiveUp.ShouldBeFalse();
        decision.RetryNumber.ShouldBe(1);
        decision.Delay.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0, 1, 5)]
    [InlineData(1, 2, 30)]
    [InlineData(2, 3, 120)]
    public void Should_wait_longer_before_each_further_retry(int retriesSoFar, int expectedRetry, int expectedSeconds)
    {
        RetryDecision decision = _policy.Decide(retriesSoFar, new TimeoutException());

        decision.RetryNumber.ShouldBe(expectedRetry);
        decision.Delay.ShouldBe(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public void Should_dead_letter_a_message_that_has_used_every_retry()
    {
        RetryDecision decision = _policy.Decide(retriesSoFar: Delays.Length, new TimeoutException());

        decision.GiveUp.ShouldBeTrue();
        decision.Reason.ShouldNotBeNull().ShouldContain("3 retries");
    }

    [Fact]
    public void Should_dead_letter_a_permanent_failure_without_spending_a_retry()
    {
        RetryDecision decision = _policy.Decide(retriesSoFar: 0, new JsonException("bad"));

        decision.GiveUp.ShouldBeTrue();
        decision.Reason.ShouldNotBeNull().ShouldContain(nameof(JsonException));
    }

    [Fact]
    public void Should_dead_letter_at_once_when_no_retries_are_configured()
    {
        var never = new RabbitMqRetryPolicy([]);

        never.Decide(retriesSoFar: 0, new TimeoutException()).GiveUp.ShouldBeTrue();
        never.MaxRetries.ShouldBe(0);
    }
}
