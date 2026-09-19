using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Abstractions.Queries;

namespace QubicaCinema.Catalog.Application.Auditoriums.GetAuditoriums;

/// <inheritdoc cref="GetAuditoriumsQuery" />
public sealed class GetAuditoriumsHandler(IAuditoriumQueries auditoriums)
    : IQueryHandler<GetAuditoriumsQuery, PagedResult<AuditoriumView>>
{
    /// <inheritdoc />
    public Task<PagedResult<AuditoriumView>> HandleAsync(
        GetAuditoriumsQuery query,
        CancellationToken cancellationToken) =>
        auditoriums.GetPageAsync(query.Skip, query.Take, cancellationToken);
}
