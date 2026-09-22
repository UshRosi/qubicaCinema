using QubicaCinema.BuildingBlocks.Api.Concurrency;
using QubicaCinema.BuildingBlocks.Api.Endpoints;
using QubicaCinema.BuildingBlocks.Api.OpenApi;
using QubicaCinema.BuildingBlocks.Api.Paging;
using QubicaCinema.BuildingBlocks.Authentication;
using QubicaCinema.BuildingBlocks.Api.Validation;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Screenings;
using QubicaCinema.Catalog.Application.Screenings.CancelScreening;
using QubicaCinema.Catalog.Application.Screenings.GetScreening;
using QubicaCinema.Catalog.Application.Screenings.GetScreenings;
using QubicaCinema.Catalog.Application.Screenings.RescheduleScreening;
using QubicaCinema.Catalog.Application.Screenings.ScheduleScreening;

namespace QubicaCinema.Catalog.Api.Screenings;

/// <summary>The programme: <c>/api/v1/screenings</c>.</summary>
/// <remarks>Anyone may read it; only an administrator may change it.</remarks>
internal sealed class ScreeningEndpoints : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder screenings = endpoints.MapGroup("/screenings").WithTags("Screenings");

        screenings.MapGet("/", ListAsync)
            .WithName("ListScreenings")
            .WithSummary("Lists the programme, filtered and ordered.");

        screenings.MapGet("/{id:guid}", GetAsync)
            .WithName("GetScreening")
            .WithSummary("Returns one screening, with the ETag needed to edit it.")
            .WithETagHeader()
            .ProducesProblem(StatusCodes.Status404NotFound);

        screenings.MapPost("/", ScheduleAsync)
            .RequiringPolicy(CinemaPolicies.Admin)
            .ValidatingBody<ScheduleScreeningRequest>()
            .WithName("ScheduleScreening")
            .WithSummary("Puts a film on the programme, if the auditorium is free.")
            .WithLocationHeader()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        screenings.MapPut("/{id:guid}", RescheduleAsync)
            .RequiringPolicy(CinemaPolicies.Admin)
            .ValidatingBody<RescheduleScreeningRequest>()
            .RequiringIfMatch()
            .WithName("RescheduleScreening")
            .WithSummary("Moves a screening or changes its price.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // A cancelled screening is still readable, and bookings still point at it, so DELETE would promise
        // something this service deliberately does not do. The cancellation is its own sub-resource.
        screenings.MapPost("/{id:guid}/cancellation", CancelAsync)
            .RequiringPolicy(CinemaPolicies.Admin)
            .ValidatingBody<CancelScreeningRequest>()
            .WithName("CancelScreening")
            .WithSummary("Calls a screening off. Asking twice is not an error.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Ok<PagedResponse<ScreeningView>>> ListAsync(
        [AsParameters] ScreeningFilterQuery filter,
        [AsParameters] PageQuery page,
        GetScreeningsHandler handler,
        CancellationToken cancellationToken)
    {
        PagedResult<ScreeningView> result = await handler.HandleAsync(
            new GetScreeningsQuery(filter.ToFilter(), page.Skip, page.Size),
            cancellationToken);

        return TypedResults.Ok(result.ToResponse(page));
    }

    private static async Task<Ok<ScreeningView>> GetAsync(
        Guid id,
        GetScreeningHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        Versioned<ScreeningView> screening = await handler.HandleAsync(new GetScreeningQuery(id), cancellationToken);
        response.SetETag(screening.Version);

        return TypedResults.Ok(screening.Value);
    }

    private static async Task<Created> ScheduleAsync(
        ScheduleScreeningRequest request,
        ScheduleScreeningHandler handler,
        CancellationToken cancellationToken)
    {
        Guid id = await handler.HandleAsync(
            new ScheduleScreeningCommand(
                request.MovieId,
                request.AuditoriumId,
                request.StartsAt,
                request.PriceAmount,
                request.PriceCurrency),
            cancellationToken);

        return TypedResults.Created($"/api/v1/screenings/{id}");
    }

    private static async Task<NoContent> RescheduleAsync(
        Guid id,
        RescheduleScreeningRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        RescheduleScreeningHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new RescheduleScreeningCommand(
                id,
                // Nullable above so that a missing header still binds: endpoint filters run after binding,
                // so a required parameter would fail before RequiringIfMatch could answer 428.
                ETag.Parse(ifMatch!),
                request.StartsAt,
                request.PriceAmount,
                request.PriceCurrency),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<CancellationAccepted>> CancelAsync(
        Guid id,
        CancelScreeningRequest request,
        CancelScreeningHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new CancelScreeningCommand(id, request.Reason), cancellationToken);

        return TypedResults.Ok(new CancellationAccepted(id));
    }
}
