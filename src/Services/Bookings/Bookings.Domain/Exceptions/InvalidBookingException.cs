using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>A booking was put together in a way no booking may be: no seats, too many screenings, or a seat twice.</summary>
public sealed class InvalidBookingException(string message) : DomainException(DomainErrorKind.Invalid, message);
