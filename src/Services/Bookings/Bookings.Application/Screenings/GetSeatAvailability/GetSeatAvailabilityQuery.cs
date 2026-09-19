namespace QubicaCinema.Bookings.Application.Screenings.GetSeatAvailability;

/// <summary>Asks which seats of a screening can be booked.</summary>
/// <param name="ScreeningId">Which screening.</param>
public sealed record GetSeatAvailabilityQuery(Guid ScreeningId);
