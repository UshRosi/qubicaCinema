using Microsoft.Extensions.Options;

namespace QubicaCinema.BuildingBlocks.Persistence.Outbox;

/// <summary>
/// The rules of <see cref="OutboxOptions"/>, including the one that annotations cannot express.
/// </summary>
/// <remarks>
/// <see cref="OutboxOptions.ClaimTimeout"/> must exceed <see cref="OutboxOptions.PollingInterval"/>, or a
/// batch would be claimed again by the next poll while it is still being published, and every message in
/// it would go out twice.
/// </remarks>
public sealed class OutboxOptionsValidator : IValidateOptions<OutboxOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, OutboxOptions options)
    {
        List<string> failures = [];

        if (options.BatchSize < 1)
        {
            failures.Add($"{nameof(OutboxOptions.BatchSize)} must be at least 1, but was {options.BatchSize}.");
        }

        if (options.PollingInterval <= TimeSpan.Zero)
        {
            failures.Add($"{nameof(OutboxOptions.PollingInterval)} must be positive, but was {options.PollingInterval}.");
        }

        if (options.ClaimTimeout <= options.PollingInterval)
        {
            failures.Add(
                $"{nameof(OutboxOptions.ClaimTimeout)} ({options.ClaimTimeout}) must be longer than " +
                $"{nameof(OutboxOptions.PollingInterval)} ({options.PollingInterval}), or a batch is claimed " +
                "again while it is still being published.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
