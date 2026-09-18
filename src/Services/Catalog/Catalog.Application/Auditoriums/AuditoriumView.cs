namespace QubicaCinema.Catalog.Application.Auditoriums;

/// <summary>
/// An auditorium in a list: its shape, but not its seats.
/// </summary>
/// <remarks>
/// A page of twenty auditoriums with full seat maps is several thousand rows to answer "what rooms are
/// there?". The seats are in <see cref="AuditoriumDetailView"/>, one auditorium at a time.
/// </remarks>
public sealed record AuditoriumView(Guid Id, string Name, int RowCount, int SeatsPerRow, int Capacity);
