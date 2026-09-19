using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>No row has enough free seats side by side for the party.</summary>
/// <remarks>
/// Deliberately not "give them whatever is left, scattered around the room": a party of four asking for
/// four seats expects to sit together, and silently splitting them is a worse outcome than a clear 409
/// that says how many seats are free, so the client can offer to choose them by hand.
/// </remarks>
public sealed class NotEnoughAdjacentSeatsException : DomainException
{
    /// <summary>Creates the exception from the screening, what was asked for, and what is left.</summary>
    public NotEnoughAdjacentSeatsException(Guid screeningId, int requested, int available)
        : base(
            DomainErrorKind.Conflict,
            available < requested
                ? $"Screening {screeningId} has {available} free seats, fewer than the {requested} requested."
                : $"Screening {screeningId} has no row with {requested} free seats side by side; choose the seats instead.")
    {
        AddExtension("screeningId", screeningId);
        AddExtension("requested", requested);
        AddExtension("available", available);
    }
}
