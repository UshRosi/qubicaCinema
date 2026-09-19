namespace QubicaCinema.Bookings.Application.Bookings.GetBookings;

/// <summary>Asks for one page of bookings.</summary>
/// <param name="UserId">
/// Whose bookings. Null means the caller's own — or, for an administrator, everyone's.
/// </param>
/// <param name="Skip">How many bookings to skip.</param>
/// <param name="Take">How many bookings to return.</param>
public sealed record GetBookingsQuery(Guid? UserId, int Skip, int Take);
