namespace QubicaCinema.BuildingBlocks.Application.Idempotency;

/// <summary>
/// What happened to a request carrying an idempotency key.
/// </summary>
/// <remarks>
/// A closed union: the private constructor means these four cases are the only ones there will ever be, so
/// the HTTP layer can match them exhaustively and a new case cannot appear without every caller being
/// revisited. Each case answers differently, which is why a boolean "was it a replay?" would not do.
/// </remarks>
public abstract record IdempotencyOutcome
{
    private IdempotencyOutcome()
    {
    }

    /// <summary>The key was new: the request ran, and its response is now recorded against the key.</summary>
    public sealed record Executed(RecordedResponse Response) : IdempotencyOutcome;

    /// <summary>The same request was made before: nothing ran, and the recorded response is returned again.</summary>
    public sealed record Replayed(RecordedResponse Response) : IdempotencyOutcome;

    /// <summary>The key was used before for a different request. A client bug, never a retry.</summary>
    public sealed record KeyReused : IdempotencyOutcome;

    /// <summary>Another request with the same key is being processed right now.</summary>
    public sealed record InFlight : IdempotencyOutcome;
}
