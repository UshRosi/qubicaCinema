namespace QubicaCinema.Catalog.Application.Auditoriums.CreateAuditorium;

/// <summary>Opens a new screening room with a grid of seats.</summary>
/// <param name="Name">What the room is called.</param>
/// <param name="RowCount">How many rows of seats it has.</param>
/// <param name="SeatsPerRow">How many seats there are in each row.</param>
public sealed record CreateAuditoriumCommand(string Name, int RowCount, int SeatsPerRow);
