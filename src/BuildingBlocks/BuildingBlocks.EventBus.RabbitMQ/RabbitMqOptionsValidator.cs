using Microsoft.Extensions.Options;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>The rules of <see cref="RabbitMqOptions"/> that annotations cannot express.</summary>
public sealed class RabbitMqOptionsValidator : IValidateOptions<RabbitMqOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RabbitMqOptions options)
    {
        // A queue's message TTL is a positive number of milliseconds; a zero or negative delay would either
        // be refused by the broker at startup or send the message straight back in a hot loop.
        if (options.RetryDelays is null || options.RetryDelays.Any(delay => delay < TimeSpan.FromMilliseconds(1)))
        {
            return ValidateOptionsResult.Fail(
                $"Every entry of {nameof(RabbitMqOptions.RetryDelays)} must be at least one millisecond.");
        }

        return ValidateOptionsResult.Success;
    }
}
