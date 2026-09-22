namespace QubicaCinema.BuildingBlocks.Api.OpenApi;

/// <summary>
/// The names of the OpenAPI documents this solution publishes: one per service.
/// </summary>
/// <remarks>
/// Declared once because the same string has to agree in three places that cannot see each other: the
/// service that publishes the document, the gateway route that proxies it, and the documentation page that
/// lists it. A test pins the gateway's routes to these constants, so renaming one breaks the build or a test
/// rather than a reviewer's dropdown.
/// </remarks>
public static class CinemaApiDocuments
{
    /// <summary>The document Catalog publishes: movies, auditoriums and screenings.</summary>
    public const string Catalog = "catalog";

    /// <summary>The document Booking publishes: bookings and the seat map.</summary>
    public const string Bookings = "bookings";

    /// <summary>The document Identity publishes: registration and login.</summary>
    public const string Identity = "identity";

    /// <summary>Every document, in the order the documentation page lists them.</summary>
    public static IReadOnlyList<string> All { get; } = [Catalog, Bookings, Identity];
}
