using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.ValueObjects;

namespace QubicaCinema.Catalog.Domain.Auditoriums;

/// <summary>
/// A screening room and the seats in it.
/// </summary>
/// <remarks>
/// The seats are the reason this is an aggregate: "a seat exists only as part of exactly one auditorium,
/// and its position is unique there" is an invariant that can only be enforced by whoever owns the whole
/// collection. So the grid is built by <see cref="Create"/>, the list is handed out read-only, and there is
/// no <c>AddSeat</c> — an auditorium cannot be given a duplicate A1 by any caller.
/// </remarks>
public sealed class Auditorium : AggregateRoot<Guid>
{
    /// <summary>The longest name the catalogue stores.</summary>
    public const int MaxNameLength = 100;

    private readonly List<Seat> _seats = [];

    private Auditorium(Guid id, string name)
        : base(id) => Name = name;

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    private Auditorium() => Name = string.Empty;

    /// <summary>What the room is called, for example <c>Sala Rossa</c>. Unique across the cinema.</summary>
    public string Name { get; private set; }

    /// <summary>How many rows of seats it has.</summary>
    public int RowCount { get; private set; }

    /// <summary>How many seats there are in each row.</summary>
    public int SeatsPerRow { get; private set; }

    /// <summary>Every seat, read-only: the collection can be looked at but never added to from outside.</summary>
    public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();

    /// <summary>How many seats the room holds in total.</summary>
    public int Capacity => RowCount * SeatsPerRow;

    /// <summary>
    /// Opens a room with a rectangular grid of seats, rows lettered from A and seats numbered from one.
    /// </summary>
    /// <remarks>
    /// A rectangle is a simplification: real auditoriums have gaps, wheelchair spaces and rows of different
    /// widths. The model is ready for it — seats are a collection of positions, not a width and a height —
    /// so an irregular layout only needs a second factory method, with nothing else changing.
    /// </remarks>
    /// <exception cref="InvalidAuditoriumLayoutException">The name is missing or the grid cannot be built.</exception>
    public static Auditorium Create(string name, int rowCount, int seatsPerRow)
    {
        var auditorium = new Auditorium(Guid.CreateVersion7(), EnsureValidName(name));
        auditorium.LayOutSeats(rowCount, seatsPerRow);

        return auditorium;
    }

    /// <summary>Finds a seat by its position, or null when the room has no such seat.</summary>
    public Seat? FindSeat(SeatPosition position) =>
        _seats.Find(seat => seat.Position == position);

    private void LayOutSeats(int rowCount, int seatsPerRow)
    {
        if (rowCount is < 1 or > SeatPosition.MaxRows)
        {
            throw new InvalidAuditoriumLayoutException(
                $"An auditorium must have between 1 and {SeatPosition.MaxRows} rows, but {rowCount} were requested.");
        }

        if (seatsPerRow is < 1 or > SeatPosition.MaxNumber)
        {
            throw new InvalidAuditoriumLayoutException(
                $"A row must hold between 1 and {SeatPosition.MaxNumber} seats, but {seatsPerRow} were requested.");
        }

        RowCount = rowCount;
        SeatsPerRow = seatsPerRow;

        for (int row = 0; row < rowCount; row++)
        {
            for (int number = 0; number < seatsPerRow; number++)
            {
                _seats.Add(new Seat(Guid.CreateVersion7(), Id, SeatPosition.FromGridIndex(row, number)));
            }
        }
    }

    private static string EnsureValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidAuditoriumLayoutException("An auditorium must have a name.");
        }

        string trimmed = name.Trim();

        return trimmed.Length <= MaxNameLength
            ? trimmed
            : throw new InvalidAuditoriumLayoutException(
                $"An auditorium name may be at most {MaxNameLength} characters, but was {trimmed.Length}.");
    }
}
