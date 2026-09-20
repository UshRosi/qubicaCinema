namespace QubicaCinema.BuildingBlocks.Contracts.Catalog;

/// <summary>One seat of the auditorium a screening takes place in.</summary>
/// <param name="SeatId">Catalog's id for the seat.</param>
/// <param name="Row">The row letter, as printed on a ticket.</param>
/// <param name="Number">The seat number within the row, counted from one.</param>
public sealed record ScreeningSeat(Guid SeatId, string Row, int Number);
