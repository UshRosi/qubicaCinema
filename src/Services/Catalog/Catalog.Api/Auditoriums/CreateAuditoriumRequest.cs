namespace QubicaCinema.Catalog.Api.Auditoriums;

/// <summary>
/// The body of <c>POST /auditoriums</c>.
/// </summary>
/// <param name="Name">What the room is called. Unique across the cinema.</param>
/// <param name="RowCount">How many rows of seats it has, lettered from A.</param>
/// <param name="SeatsPerRow">How many seats there are in each row, numbered from one.</param>
public sealed record CreateAuditoriumRequest(string Name, int RowCount, int SeatsPerRow);
