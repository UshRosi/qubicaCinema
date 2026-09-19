
using QubicaCinema.Catalog.Domain.ValueObjects;

namespace QubicaCinema.Catalog.Domain.Screenings;

/// <summary>
/// One occupied slot in an auditorium's day: which screening holds it, and when.
/// </summary>
/// <remarks>
/// Overlap is a rule between screenings, but loading every competing <see cref="Screening"/> to check it
/// would pull whole aggregates out of the database to read two dates. This is what the repository projects
/// instead, and it is what <see cref="Screening.Schedule"/> asks for — so the invariant stays in the model
/// while the query stays cheap.
/// </remarks>
/// <param name="ScreeningId">The screening occupying the slot.</param>
/// <param name="Slot">When the auditorium is busy, cleaning time included.</param>
public sealed record ScheduledSlot(Guid ScreeningId, TimeSlot Slot);
