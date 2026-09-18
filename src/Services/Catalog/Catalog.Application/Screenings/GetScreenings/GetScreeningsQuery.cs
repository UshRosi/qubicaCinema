namespace QubicaCinema.Catalog.Application.Screenings.GetScreenings;

/// <summary>Asks for one page of the programme.</summary>
/// <param name="Filter">How to narrow and order it.</param>
/// <param name="Skip">How many screenings to skip.</param>
/// <param name="Take">How many screenings to return.</param>
public sealed record GetScreeningsQuery(ScreeningFilter Filter, int Skip, int Take);
