namespace QubicaCinema.BuildingBlocks.Application.Results;

/// <summary>
/// One page of read models, and how many there are in total.
/// </summary>
/// <remarks>
/// Deliberately not the API's response envelope: that one carries page numbers and links, which are HTTP
/// concerns. This carries only what a query can actually know.
/// </remarks>
/// <typeparam name="T">The read model.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="TotalCount">How many items match in total.</param>
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int TotalCount);
