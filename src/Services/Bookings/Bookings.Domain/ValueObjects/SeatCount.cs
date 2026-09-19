using System.Globalization;
using QubicaCinema.Bookings.Domain.Exceptions;

namespace QubicaCinema.Bookings.Domain.ValueObjects;

/// <summary>
/// How many seats a customer asks for at one screening.
/// </summary>
/// <remarks>
/// A value object rather than an <see cref="int"/> so that "between one and ten" is checked once, when the
/// number enters the model, instead of at every place that receives one. The upper bound is a business
/// rule — a group bigger than ten is a private hire, not a booking — and it also caps how much of an
/// auditorium one request can take off the market.
/// </remarks>
public sealed record SeatCount
{
    /// <summary>The fewest seats one can ask for.</summary>
    public const int Min = 1;

    /// <summary>The most seats one booking can hold at one screening.</summary>
    public const int Max = 10;

    private SeatCount(int value) => Value = value;

    /// <summary>The number of seats.</summary>
    public int Value { get; }

    /// <summary>Creates a count, rejecting one that is out of range.</summary>
    /// <exception cref="InvalidSeatCountException">The count is below <see cref="Min"/> or above <see cref="Max"/>.</exception>
    public static SeatCount Of(int value) =>
        value is >= Min and <= Max
            ? new SeatCount(value)
            : throw new InvalidSeatCountException(value);

    /// <summary>Renders the count, for logs and traces.</summary>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
