using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Abstractions.Queries;

namespace QubicaCinema.Catalog.Application.Screenings.GetScreenings;

/// <inheritdoc cref="GetScreeningsQuery" />
public sealed class GetScreeningsHandler(IScreeningQueries screenings)
    : IQueryHandler<GetScreeningsQuery, PagedResult<ScreeningView>>
{
    /// <inheritdoc />
    public Task<PagedResult<ScreeningView>> HandleAsync(
        GetScreeningsQuery query,
        CancellationToken cancellationToken) =>
        screenings.GetPageAsync(query.Filter, query.Skip, query.Take, cancellationToken);
}
