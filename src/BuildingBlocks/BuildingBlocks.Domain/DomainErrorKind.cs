namespace QubicaCinema.BuildingBlocks.Domain;

/// <summary>
/// What kind of rule a <see cref="DomainException"/> broke.
/// </summary>
/// <remarks>
/// The domain says what went wrong in its own words; the API layer alone decides which status code that
/// becomes. That keeps HTTP out of the model while still giving one exception handler enough to map every
/// domain failure without a chain of type checks.
/// </remarks>
public enum DomainErrorKind
{
    /// <summary>The thing being acted on does not exist, or the caller may not know that it does.</summary>
    NotFound,

    /// <summary>The request is well formed but the current state refuses it, such as a seat already taken.</summary>
    Conflict,

    /// <summary>The request breaks a rule that can be decided from its own content.</summary>
    Invalid,

    /// <summary>The caller is known but is not allowed to do this.</summary>
    Forbidden,

    /// <summary>The caller could not be identified: bad credentials. Distinct from <see cref="Forbidden"/>, which is a known caller refused.</summary>
    Unauthenticated,

    /// <summary>
    /// The caller stated what it expected the current state to be, and it was wrong — a stale
    /// <c>If-Match</c>. Distinct from <see cref="Conflict"/>: retrying is pointless until the caller
    /// re-reads, which is exactly what 412 tells it to do.
    /// </summary>
    PreconditionFailed,
}
