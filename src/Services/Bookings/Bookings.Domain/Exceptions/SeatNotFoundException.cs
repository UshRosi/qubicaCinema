using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>A seat was asked for that is not in the auditorium where the screening takes place.</summary>
/// <remarks>
/// 404 rather than 422: whether a seat id belongs to a room can only be decided by looking the room up,
/// and the litmus test for 422 is "decidable from the payload alone".
/// </remarks>
public sealed class SeatNotFoundException : DomainException
{
    /// <summary>Creates the exception from the screening and the seats it does not have.</summary>
    public SeatNotFoundException(Guid screeningId, IReadOnlyCollection<Guid> seatIds)
        : base(
            DomainErrorKind.NotFound,
            $"Screening {screeningId} has no seat with id {string.Join(", ", seatIds)}.")
    {
        AddExtension("screeningId", screeningId);
        AddExtension("unknownSeatIds", seatIds);
    }
}
