namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>What to do with a message whose handler failed.</summary>
public sealed record RetryDecision
{
    private RetryDecision()
    {
    }

    /// <summary>Whether the message is given up on and sent to the dead-letter queue.</summary>
    public bool GiveUp { get; private init; }

    /// <summary>For a retry, which one it is, counting from one.</summary>
    public int RetryNumber { get; private init; }

    /// <summary>For a retry, how long it waits first.</summary>
    public TimeSpan Delay { get; private init; }

    /// <summary>Why the message is given up on.</summary>
    public string? Reason { get; private init; }

    /// <summary>Try again after <paramref name="delay"/>.</summary>
    public static RetryDecision RetryAfter(int retryNumber, TimeSpan delay) =>
        new() { RetryNumber = retryNumber, Delay = delay };

    /// <summary>Send it to the dead-letter queue.</summary>
    public static RetryDecision DeadLetter(string reason) => new() { GiveUp = true, Reason = reason };
}
