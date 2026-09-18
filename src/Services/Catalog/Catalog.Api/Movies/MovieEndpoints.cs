using QubicaCinema.BuildingBlocks.Api.Concurrency;
using QubicaCinema.BuildingBlocks.Api.Endpoints;
using QubicaCinema.BuildingBlocks.Api.Paging;
using QubicaCinema.BuildingBlocks.Api.Validation;
using QubicaCinema.BuildingBlocks.Application.Results;
using QubicaCinema.Catalog.Application.Movies;
using QubicaCinema.Catalog.Application.Movies.CreateMovie;
using QubicaCinema.Catalog.Application.Movies.GetMovie;
using QubicaCinema.Catalog.Application.Movies.GetMovies;
using QubicaCinema.Catalog.Application.Movies.UpdateMovie;

namespace QubicaCinema.Catalog.Api.Movies;

/// <summary>The film catalogue: <c>/api/v1/movies</c>.</summary>
/// <remarks>
/// Writing is an administrator's job. The routes are open in this chapter and gain
/// <c>RequireAuthorization</c> in chapter 5, when there is an identity to check.
/// </remarks>
internal sealed class MovieEndpoints : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder movies = endpoints.MapGroup("/movies").WithTags("Movies");

        movies.MapGet("/", ListAsync)
            .WithSummary("Lists the films in the catalogue.");

        movies.MapGet("/{id:guid}", GetAsync)
            .WithSummary("Returns one film, with the ETag needed to edit it.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        movies.MapPost("/", CreateAsync)
            .ValidatingBody<SaveMovieRequest>()
            .WithSummary("Adds a film to the catalogue.");

        movies.MapPut("/{id:guid}", UpdateAsync)
            .ValidatingBody<SaveMovieRequest>()
            .RequiringIfMatch()
            .WithSummary("Corrects a film, provided nobody has edited it since it was read.")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<PagedResponse<MovieView>>> ListAsync(
        [AsParameters] PageQuery page,
        GetMoviesHandler handler,
        CancellationToken cancellationToken)
    {
        PagedResult<MovieView> result =
            await handler.HandleAsync(new GetMoviesQuery(page.Skip, page.Size), cancellationToken);

        return TypedResults.Ok(result.ToResponse(page));
    }

    private static async Task<Ok<MovieView>> GetAsync(
        Guid id,
        GetMovieHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        Versioned<MovieView> movie = await handler.HandleAsync(new GetMovieQuery(id), cancellationToken);
        response.SetETag(movie.Version);

        return TypedResults.Ok(movie.Value);
    }

    private static async Task<Created> CreateAsync(
        SaveMovieRequest request,
        CreateMovieHandler handler,
        CancellationToken cancellationToken)
    {
        Guid id = await handler.HandleAsync(
            new CreateMovieCommand(
                request.Title,
                request.Description ?? string.Empty,
                request.DurationMinutes,
                request.Genre,
                request.AgeRating),
            cancellationToken);

        return TypedResults.Created($"/api/v1/movies/{id}");
    }

    private static async Task<NoContent> UpdateAsync(
        Guid id,
        SaveMovieRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        UpdateMovieHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new UpdateMovieCommand(
                id,
                // Nullable so that a missing header still binds: endpoint filters run after binding, so a
                // required parameter would fail before RequiringIfMatch could answer 428. By the time this
                // runs the filter has already accepted the header, which is what keeps the method free of
                // an if.
                ETag.Parse(ifMatch!),
                request.Title,
                request.Description ?? string.Empty,
                request.DurationMinutes,
                request.Genre,
                request.AgeRating),
            cancellationToken);

        return TypedResults.NoContent();
    }
}
