namespace QubicaCinema.BuildingBlocks.Domain;

/// <summary>
/// Someone else changed the thing being edited between the caller reading it and writing it back.
/// </summary>
/// <remarks>
/// Raised by Infrastructure when the store reports a version conflict, so that Application code and the
/// endpoints never see a persistence exception. This is the lost-update guard behind <c>If-Match</c>: the
/// caller is told to re-read rather than being allowed to overwrite a change it never saw.
/// </remarks>
public sealed class ConcurrentModificationException : DomainException
{
    /// <summary>Creates the exception from the resource that had moved on.</summary>
    public ConcurrentModificationException(string resource, object id)
        : base(
            DomainErrorKind.PreconditionFailed,
            $"{resource} {id} was modified by someone else; read it again and retry.")
    {
        AddExtension("resource", resource);
        AddExtension("id", id);
    }

    /// <summary>Creates the exception from the store's own concurrency failure.</summary>
    public ConcurrentModificationException(string resource, object id, Exception innerException)
        : base(
            DomainErrorKind.PreconditionFailed,
            $"{resource} {id} was modified by someone else; read it again and retry.",
            innerException)
    {
        AddExtension("resource", resource);
        AddExtension("id", id);
    }
}
