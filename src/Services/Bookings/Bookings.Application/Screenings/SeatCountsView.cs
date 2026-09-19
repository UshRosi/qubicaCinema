namespace QubicaCinema.Bookings.Application.Screenings;

/// <summary>
/// How full a screening is — the part of the seat map a client booking "any three seats" actually needs.
/// </summary>
/// <param name="Total">Every seat in the auditorium.</param>
/// <param name="Available">Seats nobody holds.</param>
/// <param name="Booked">Seats held by an active booking.</param>
public sealed record SeatCountsView(int Total, int Available, int Booked);
