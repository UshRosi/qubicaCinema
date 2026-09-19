using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>A customer asked to list another user's bookings, which only an administrator may do.</summary>
/// <remarks>
/// 403, not 404, and not a silent fall-back to the caller's own list. Nothing is revealed by refusing — the
/// caller already named the user — and quietly ignoring the parameter would answer a question that was not
/// asked with a list that looks like the answer.
/// </remarks>
public sealed class BookingListForbiddenException : DomainException
{
    /// <summary>Creates the exception from the user whose bookings were asked for.</summary>
    public BookingListForbiddenException(Guid requestedUserId)
        : base(DomainErrorKind.Forbidden, "Only an administrator may list another user's bookings.") =>
        AddExtension("requestedUserId", requestedUserId);
}
