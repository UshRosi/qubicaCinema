using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.Catalog.Application.Abstractions.Queries;
using QubicaCinema.Catalog.Domain.Exceptions;

namespace QubicaCinema.Catalog.Application.Auditoriums.GetAuditorium;

/// <inheritdoc cref="GetAuditoriumQuery" />
public sealed class GetAuditoriumHandler(IAuditoriumQueries auditoriums)
    : IQueryHandler<GetAuditoriumQuery, AuditoriumDetailView>
{
    /// <inheritdoc />
    /// <exception cref="AuditoriumNotFoundException">No auditorium has that id.</exception>
    public async Task<AuditoriumDetailView> HandleAsync(
        GetAuditoriumQuery query,
        CancellationToken cancellationToken) =>
        await auditoriums.FindAsync(query.AuditoriumId, cancellationToken)
        ?? throw new AuditoriumNotFoundException(query.AuditoriumId);
}
