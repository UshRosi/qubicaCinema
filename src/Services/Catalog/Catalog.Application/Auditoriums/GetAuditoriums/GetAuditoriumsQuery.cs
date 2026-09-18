namespace QubicaCinema.Catalog.Application.Auditoriums.GetAuditoriums;

/// <summary>Asks for one page of the auditorium list.</summary>
public sealed record GetAuditoriumsQuery(int Skip, int Take);
