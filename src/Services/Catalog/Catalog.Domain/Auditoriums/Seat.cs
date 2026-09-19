using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Domain.ValueObjects;

namespace QubicaCinema.Catalog.Domain.Auditoriums;

/// <summary>
/// One physical seat in one auditorium.
/// </summary>
/// <remarks>
/// An entity rather than a value object, because Booking refers to a seat by id for years after the
/// catalogue was written, and a row could be relettered without the booking changing meaning. It belongs to
/// the <see cref="Auditorium"/> aggregate: only the auditorium creates seats, which is why the constructor
/// is internal and there is no public way to add one.
/// </remarks>
public sealed class Seat : Entity<Guid>
{
    internal Seat(Guid id, Guid auditoriumId, SeatPosition position)
        : base(id)
    {
        AuditoriumId = auditoriumId;
        Position = position;
    }

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private Seat() => Position = null!;

    /// <summary>The auditorium this seat is bolted to.</summary>
    public Guid AuditoriumId { get; private set; }

    /// <summary>Where the seat is, as printed on a ticket.</summary>
    public SeatPosition Position { get; private set; }

    /// <summary>Renders the seat the way a ticket does, for logs and traces.</summary>
    public override string ToString() => Position.ToString();
}
