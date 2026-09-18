namespace QubicaCinema.BuildingBlocks.Api.Paging;

/// <summary>
/// One page of results, with enough context for a client to page through the rest.
/// </summary>
/// <remarks>
/// An envelope rather than a bare array: a list endpoint that returns <c>[…]</c> has nowhere to put the
/// total or the page size, and adding them later is a breaking change for every client already parsing it.
/// </remarks>
/// <typeparam name="T">What the page contains.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="Page">Which page this is, one-based.</param>
/// <param name="PageSize">How many items a full page holds.</param>
/// <param name="TotalCount">How many items match in total, across every page.</param>
public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>How many pages the full result set spans.</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether asking for the next page is worth the round trip.</summary>
    public bool HasNextPage => Page < TotalPages;
}
