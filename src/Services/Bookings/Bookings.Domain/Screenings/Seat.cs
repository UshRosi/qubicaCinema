using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Bookings.Domain.Screenings;

/// <summary>
/// One physical seat in one auditorium, as Catalog announced it.
/// </summary>
/// <remarks>
/// It belongs to the auditorium, not to a screening: the same seat is offered at every screening in the
/// room. Whether it is free is a fact about a screening, and lives in <see cref="SeatMap"/>.
/// </remarks>
public sealed class Seat : Entity<Guid>
{
    private Seat(Guid id, Guid auditoriumId, SeatPosition position)
        : base(id)
    {
        AuditoriumId = auditoriumId;
        Position = position;
    }

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private Seat() => Position = null!;

    /// <summary>The auditorium this seat is in.</summary>
    public Guid AuditoriumId { get; private set; }

    /// <summary>Where the seat is, as printed on a ticket.</summary>
    public SeatPosition Position { get; private set; }

    /// <summary>Records a seat that Catalog laid out.</summary>
    /// <param name="id">Catalog's id for the seat.</param>
    /// <param name="auditoriumId">Catalog's id for its auditorium.</param>
    /// <param name="position">Where it is.</param>
    public static Seat Create(Guid id, Guid auditoriumId, SeatPosition position) => new(id, auditoriumId, position);

    /// <summary>Renders the seat the way a ticket does, for logs and traces.</summary>
    public override string ToString() => Position.ToString();
}
