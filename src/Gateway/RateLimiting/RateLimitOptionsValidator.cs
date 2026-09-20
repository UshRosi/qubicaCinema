using Microsoft.Extensions.Options;

namespace QubicaCinema.Gateway.RateLimiting;

/// <summary>
/// The rules of <see cref="RateLimitOptions"/>, including the one that annotations cannot express.
/// </summary>
/// <remarks>
/// The booking budget must be smaller than the global one. Otherwise the named policy is a no-op that reads
/// like protection, because the global limiter would always refuse first.
/// </remarks>
internal sealed class RateLimitOptionsValidator : IValidateOptions<RateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitOptions options)
    {
        List<string> failures = [];

        if (options.PermitLimit < 1)
        {
            failures.Add($"{nameof(RateLimitOptions.PermitLimit)} must be at least 1, but was {options.PermitLimit}.");
        }

        if (options.Window <= TimeSpan.Zero)
        {
            failures.Add($"{nameof(RateLimitOptions.Window)} must be positive, but was {options.Window}.");
        }

        if (options.BookingCreationPermitLimit < 1)
        {
            failures.Add(
                $"{nameof(RateLimitOptions.BookingCreationPermitLimit)} must be at least 1, " +
                $"but was {options.BookingCreationPermitLimit}.");
        }

        if (options.BookingCreationWindow <= TimeSpan.Zero)
        {
            failures.Add(
                $"{nameof(RateLimitOptions.BookingCreationWindow)} must be positive, " +
                $"but was {options.BookingCreationWindow}.");
        }

        if (options.BookingCreationPermitLimit >= options.PermitLimit)
        {
            failures.Add(
                $"{nameof(RateLimitOptions.BookingCreationPermitLimit)} ({options.BookingCreationPermitLimit}) " +
                $"must be smaller than {nameof(RateLimitOptions.PermitLimit)} ({options.PermitLimit}), or the " +
                "stricter booking policy never refuses anything the global limiter has not refused already.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
