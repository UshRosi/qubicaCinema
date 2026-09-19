namespace QubicaCinema.Catalog.Application.Auditoriums;

/// <summary>One auditorium with every seat in it.</summary>
public sealed record AuditoriumDetailView(
    Guid Id,
    string Name,
    int RowCount,
    int SeatsPerRow,
    int Capacity,
    IReadOnlyCollection<SeatView> Seats);
