namespace QubicaCinema.Catalog.Application.Screenings;

/// <summary>The orderings a screening list can be served in.</summary>
public enum ScreeningSort
{
    /// <summary>Next one first. What a programme shows.</summary>
    StartingSoonest = 0,

    /// <summary>Latest first.</summary>
    StartingLatest,

    /// <summary>Cheapest seat first.</summary>
    CheapestFirst,

    /// <summary>Dearest seat first.</summary>
    DearestFirst,
}
