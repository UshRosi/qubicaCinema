namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>Waits on a fact instead of a fixed delay, for the one thing in this tier that is asynchronous by nature.</summary>
/// <remarks>
/// Everything else here is deterministic: the outbox is pumped by hand and the database is queried directly.
/// The exception is the real RabbitMQ round trip from Catalog's outbox to Booking's inbox, which this helper
/// polls for rather than sleeping a guessed number of seconds.
/// </remarks>
internal static class Eventually
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>Polls <paramref name="probe"/> until it returns true or <paramref name="timeout"/> elapses.</summary>
    /// <param name="probe">Returns whether the awaited fact now holds.</param>
    /// <param name="because">What is being waited for, used only in the timeout message.</param>
    /// <param name="timeout">How long to keep trying.</param>
    internal static async Task UntilAsync(Func<Task<bool>> probe, string because, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await probe())
            {
                return;
            }

            await Task.Delay(PollInterval);
        }

        throw new TimeoutException($"Gave up after {timeout} waiting for: {because}");
    }
}
