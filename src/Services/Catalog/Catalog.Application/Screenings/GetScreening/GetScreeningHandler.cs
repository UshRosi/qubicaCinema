using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Abstractions.Queries;
using QubicaCinema.Catalog.Domain.Exceptions;

namespace QubicaCinema.Catalog.Application.Screenings.GetScreening;

/// <inheritdoc cref="GetScreeningQuery" />
public sealed class GetScreeningHandler(IScreeningQueries screenings)
    : IQueryHandler<GetScreeningQuery, Versioned<ScreeningView>>
{
    /// <inheritdoc />
    /// <exception cref="ScreeningNotFoundException">No screening has that id.</exception>
    public async Task<Versioned<ScreeningView>> HandleAsync(
        GetScreeningQuery query,
        CancellationToken cancellationToken) =>
        await screenings.FindAsync(query.ScreeningId, cancellationToken)
        ?? throw new ScreeningNotFoundException(query.ScreeningId);
}
