using QubicaCinema.BuildingBlocks.Api.Endpoints;
using QubicaCinema.BuildingBlocks.Api.Paging;
using QubicaCinema.BuildingBlocks.Api.Validation;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Auditoriums;
using QubicaCinema.Catalog.Application.Auditoriums.CreateAuditorium;
using QubicaCinema.Catalog.Application.Auditoriums.GetAuditorium;
using QubicaCinema.Catalog.Application.Auditoriums.GetAuditoriums;

namespace QubicaCinema.Catalog.Api.Auditoriums;

/// <summary>The screening rooms: <c>/api/v1/auditoriums</c>.</summary>
/// <remarks>
/// There is no update and no delete. A room's seat grid is referenced by every booking ever made in it, so
/// changing it in place would rewrite history; a rebuilt room is a new auditorium.
/// </remarks>
internal sealed class AuditoriumEndpoints : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder auditoriums = endpoints.MapGroup("/auditoriums").WithTags("Auditoriums");

        auditoriums.MapGet("/", ListAsync)
            .WithSummary("Lists the screening rooms, without their seat maps.");

        auditoriums.MapGet("/{id:guid}", GetAsync)
            .WithSummary("Returns one room and every seat in it.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        auditoriums.MapPost("/", CreateAsync)
            .ValidatingBody<CreateAuditoriumRequest>()
            .WithSummary("Opens a room and lays out its grid of seats.")
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Ok<PagedResponse<AuditoriumView>>> ListAsync(
        [AsParameters] PageQuery page,
        GetAuditoriumsHandler handler,
        CancellationToken cancellationToken)
    {
        PagedResult<AuditoriumView> result =
            await handler.HandleAsync(new GetAuditoriumsQuery(page.Skip, page.Size), cancellationToken);

        return TypedResults.Ok(result.ToResponse(page));
    }

    private static async Task<Ok<AuditoriumDetailView>> GetAsync(
        Guid id,
        GetAuditoriumHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(new GetAuditoriumQuery(id), cancellationToken));

    private static async Task<Created> CreateAsync(
        CreateAuditoriumRequest request,
        CreateAuditoriumHandler handler,
        CancellationToken cancellationToken)
    {
        Guid id = await handler.HandleAsync(
            new CreateAuditoriumCommand(request.Name, request.RowCount, request.SeatsPerRow),
            cancellationToken);

        return TypedResults.Created($"/api/v1/auditoriums/{id}");
    }
}
