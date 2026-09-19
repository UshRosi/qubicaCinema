
using QubicaCinema.Bookings.Domain.ValueObjects;

namespace QubicaCinema.Bookings.Domain.SeatAllocation;

/// <summary>
/// How a customer chooses seats at one screening: by naming them, or by saying how many.
/// </summary>
/// <remarks>
/// A closed union. The private constructor means the two nested records are the only cases there can ever
/// be, so "seats <em>or</em> a quantity, never both, never neither" is a fact of the type system rather
/// than a validation rule someone might forget. The records are nested because a closed union needs them
/// to be: only a type declared inside can reach the private constructor.
/// </remarks>
public abstract record SeatSelection
{
    private SeatSelection()
    {
    }

    /// <summary>These exact seats.</summary>
    /// <param name="SeatIds">The seats, by id.</param>
    public sealed record Explicit(IReadOnlyList<Guid> SeatIds) : SeatSelection;

    /// <summary>This many seats, wherever the allocation strategy finds them best.</summary>
    /// <param name="Count">How many.</param>
    public sealed record ByQuantity(SeatCount Count) : SeatSelection;
}
