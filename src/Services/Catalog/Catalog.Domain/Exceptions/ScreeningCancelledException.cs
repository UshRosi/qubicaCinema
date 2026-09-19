using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>The screening was cancelled, so it can no longer be changed.</summary>
/// <remarks>
/// Cancelling an already cancelled screening is <em>not</em> this: that is idempotent and succeeds quietly.
/// This is raised only by an attempt to move or reprice something that is no longer running.
/// </remarks>
public sealed class ScreeningCancelledException : DomainException
{
    /// <summary>Creates the exception from the screening that was cancelled.</summary>
    public ScreeningCancelledException(Guid screeningId)
        : base(DomainErrorKind.Conflict, $"Screening {screeningId} was cancelled and can no longer change.") =>
        AddExtension("screeningId", screeningId);
}
