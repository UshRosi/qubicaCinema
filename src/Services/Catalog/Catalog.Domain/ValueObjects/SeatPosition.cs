using System.Globalization;
using QubicaCinema.Catalog.Domain.Exceptions;

namespace QubicaCinema.Catalog.Domain.ValueObjects;

/// <summary>
/// Where a seat is: a row letter and a number within that row, as printed on a ticket.
/// </summary>
/// <remarks>
/// A position is not an identity. Two auditoriums both have an A1, and comparing seats by position across
/// auditoriums is meaningless — that is what the seat's id is for. Equality here is structural, which is
/// exactly what is wanted when checking that a booking request names a seat the auditorium really has.
/// </remarks>
public sealed record SeatPosition
{
    /// <summary>The highest row an auditorium can have, because rows are single letters A to Z.</summary>
    public const int MaxRows = 26;

    /// <summary>The highest seat number in a row. Generous, but it stops a typo creating a million seats.</summary>
    public const int MaxNumber = 100;

    private SeatPosition(string row, int number)
    {
        Row = row;
        Number = number;
    }

    /// <summary>The row, a single upper-case letter.</summary>
    public string Row { get; }

    /// <summary>The seat number within the row, starting at one.</summary>
    public int Number { get; }

    /// <summary>Creates a position from a row letter and a seat number.</summary>
    /// <exception cref="InvalidSeatPositionException">The row is not a single letter, or the number is out of range.</exception>
    public static SeatPosition Of(string row, int number)
    {
        if (string.IsNullOrWhiteSpace(row) || row.Length != 1 || !char.IsAsciiLetter(row[0]))
        {
            throw new InvalidSeatPositionException($"'{row}' is not a seat row; a single letter A to Z was expected.");
        }

        if (number is < 1 or > MaxNumber)
        {
            throw new InvalidSeatPositionException(
                $"Seat number {number} is out of range; it must be between 1 and {MaxNumber}.");
        }

        return new SeatPosition(row.ToUpperInvariant(), number);
    }

    /// <summary>
    /// Creates the position of the n-th seat in the n-th row of a generated grid, both counted from zero.
    /// </summary>
    internal static SeatPosition FromGridIndex(int rowIndex, int numberIndex) =>
        Of(((char)('A' + rowIndex)).ToString(), numberIndex + 1);

    /// <summary>Renders the position the way a ticket does, for example <c>C7</c>.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Row}{Number}");
}
