using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Exceptions;

/// <summary>
/// One or more of the requested seats are already booked for that screening.
/// </summary>
/// <remarks>
/// Raised in two places for the same rule. The model raises it when the seat map it was given already
/// shows the seat as taken — the common case, with a clear message. Infrastructure raises it when the
/// filtered unique index rejects the insert — the race between two requests that both saw the seat free,
/// which only the database can settle. Either way the client receives the ids it has to choose again,
/// in the ProblemDetails extensions, without parsing the message.
/// </remarks>
public sealed class SeatAlreadyBookedException : DomainException
{
    /// <summary>Creates the exception from the seats the model found taken.</summary>
    public SeatAlreadyBookedException(Guid screeningId, IReadOnlyCollection<Guid> unavailableSeatIds)
        : base(DomainErrorKind.Conflict, Describe(screeningId, unavailableSeatIds))
    {
        ScreeningId = screeningId;
        UnavailableSeatIds = unavailableSeatIds;
        AddExtension("screeningId", screeningId);
        AddExtension("unavailableSeatIds", unavailableSeatIds);
    }

    /// <summary>Creates the exception from the index violation that revealed the clash.</summary>
    public SeatAlreadyBookedException(
        Guid screeningId,
        IReadOnlyCollection<Guid> unavailableSeatIds,
        Exception innerException)
        : base(DomainErrorKind.Conflict, Describe(screeningId, unavailableSeatIds), innerException)
    {
        ScreeningId = screeningId;
        UnavailableSeatIds = unavailableSeatIds;
        AddExtension("screeningId", screeningId);
        AddExtension("unavailableSeatIds", unavailableSeatIds);
    }

    /// <summary>The screening the seats were requested for.</summary>
    public Guid ScreeningId { get; }

    /// <summary>The seats that are no longer available.</summary>
    public IReadOnlyCollection<Guid> UnavailableSeatIds { get; }

    private static string Describe(Guid screeningId, IReadOnlyCollection<Guid> seatIds) =>
        $"{seatIds.Count} of the requested seats for screening {screeningId} are already booked.";
}
