namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// Chooses between retrying a failed message and dead-lettering it.
/// </summary>
/// <remarks>
/// Pure: no broker, no clock, only the message's retry count and the exception. That is what lets the rules
/// be unit tested exhaustively, while the messy part — moving a message between queues — stays in the
/// consumer.
/// </remarks>
public sealed class RabbitMqRetryPolicy(IReadOnlyList<TimeSpan> retryDelays)
{
    /// <summary>How many retries a message gets before it is given up on.</summary>
    public int MaxRetries => retryDelays.Count;

    /// <summary>
    /// Decides what happens to a message that has already been retried <paramref name="retriesSoFar"/> times
    /// and has just failed again with <paramref name="exception"/>.
    /// </summary>
    public RetryDecision Decide(int retriesSoFar, Exception exception)
    {
        if (EventFailureClassifier.IsPermanent(exception))
        {
            return RetryDecision.DeadLetter($"Permanent failure: {exception.GetType().Name}.");
        }

        if (retriesSoFar >= MaxRetries)
        {
            return RetryDecision.DeadLetter($"Still failing after {retriesSoFar} retries.");
        }

        return RetryDecision.RetryAfter(retriesSoFar + 1, retryDelays[retriesSoFar]);
    }
}
