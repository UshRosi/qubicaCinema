using QubicaCinema.Bookings.Domain.ValueObjects;
using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>A number of seats was asked for that one booking cannot hold.</summary>
public sealed class InvalidSeatCountException : DomainException
{
    /// <summary>Creates the exception from the count that was asked for.</summary>
    public InvalidSeatCountException(int requested)
        : base(
            DomainErrorKind.Invalid,
            $"Between {SeatCount.Min} and {SeatCount.Max} seats can be booked at one screening, but {requested} were requested.") =>
        AddExtension("requested", requested);
}
