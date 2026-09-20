using QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

namespace QubicaCinema.BuildingBlocks.UnitTests.Messaging;

public sealed class RabbitMqOptionsValidatorTests
{
    private readonly RabbitMqOptionsValidator _validator = new();

    [Fact]
    public void Should_accept_the_defaults() =>
        _validator.Validate(null, new RabbitMqOptions()).Succeeded.ShouldBeTrue();

    [Fact]
    public void Should_accept_no_retries_at_all() =>
        _validator.Validate(null, new RabbitMqOptions { RetryDelays = [] }).Succeeded.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void Should_refuse_a_delay_that_is_not_positive(int milliseconds) =>
        _validator
            .Validate(null, new RabbitMqOptions { RetryDelays = [TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(milliseconds)] })
            .Failed.ShouldBeTrue();
}
