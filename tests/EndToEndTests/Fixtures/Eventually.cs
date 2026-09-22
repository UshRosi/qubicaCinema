namespace QubicaCinema.EndToEndTests.Fixtures;

/// <summary>Waits on a fact instead of a fixed delay, for the one step in a flow that is asynchronous by nature.</summary>
/// <remarks>
/// Duplicated from <c>tests/Services.IntegrationTests/Fixtures/Eventually.cs</c> rather than shared: this
/// solution has no common test project, on purpose.
/// </remarks>
internal static class Eventually
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

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
