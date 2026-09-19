using QubicaCinema.BuildingBlocks.Application.Security;

namespace QubicaCinema.Bookings.Application.Bookings;

/// <summary>Who may see and change a booking.</summary>
/// <remarks>
/// Enforced in the use cases, not in the endpoints, because it needs the loaded booking to know who owns
/// it. When the answer is no, the use cases throw <c>BookingNotFoundException</c> — a 404, not a 403 — so
/// that a customer cannot learn whether somebody else's booking id exists by asking for it.
/// </remarks>
internal static class BookingAccess
{
    /// <summary>The owner may, and so may an administrator acting for the cinema.</summary>
    internal static bool MayAccess(this ICurrentUser user, Guid ownerId) =>
        user.IsAdministrator || user.UserId == ownerId;
}
