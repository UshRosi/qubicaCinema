using QubicaCinema.BuildingBlocks.Application.Results;

namespace QubicaCinema.BuildingBlocks.Api.Paging;

/// <summary>Turns what a query found into what the HTTP response carries.</summary>
public static class PagedResultExtensions
{
    /// <summary>
    /// Wraps a page of items in the response envelope, taking the page number and size from the request.
    /// </summary>
    /// <remarks>
    /// The two types are deliberately different: the use case knows how many rows matched, only the HTTP
    /// layer knows which page was asked for. This is where the two meet.
    /// </remarks>
    public static PagedResponse<T> ToResponse<T>(this PagedResult<T> result, PageQuery query) =>
        new(result.Items, query.Number, query.Size, result.TotalCount);
}
