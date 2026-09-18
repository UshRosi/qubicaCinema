namespace QubicaCinema.Catalog.Application.Auditoriums;

/// <summary>One seat, as the seat map shows it.</summary>
public sealed record SeatView(Guid Id, string Row, int Number);
