namespace QubicaCinema.BuildingBlocks.Domain;

/// <summary>
/// The base of every failure the model raises on purpose.
/// </summary>
/// <remarks>
/// Each concrete subclass names one broken rule, so a use case never inspects messages to decide what
/// happened, and one exception handler turns every one of them into a ProblemDetails response. Because the
/// mapping lives in a single place, no endpoint and no handler needs a try/catch.
/// </remarks>
public abstract class DomainException : Exception
{
    private readonly Dictionary<string, object?> _extensions = [];

    /// <summary>Creates the exception with the kind of rule that was broken.</summary>
    protected DomainException(DomainErrorKind kind, string message)
        : base(message) => Kind = kind;

    /// <summary>Creates the exception from an underlying failure, such as a unique-index violation.</summary>
    protected DomainException(DomainErrorKind kind, string message, Exception innerException)
        : base(message, innerException) => Kind = kind;

    /// <summary>What kind of rule was broken.</summary>
    public DomainErrorKind Kind { get; }

    /// <summary>
    /// Machine-readable details copied into the ProblemDetails extensions, for example the ids of the seats
    /// that were taken. Lets a client react without parsing the message.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Extensions => _extensions;

    /// <summary>Adds one machine-readable detail. Called by a subclass while constructing itself.</summary>
    protected void AddExtension(string name, object? value) => _extensions[name] = value;
}
