using QubicaCinema.BuildingBlocks.Api.Endpoints;
using QubicaCinema.BuildingBlocks.Api.Validation;

namespace QubicaCinema.Identity.Api.Auth;

/// <summary>Registration and login: <c>/api/v1/auth</c>.</summary>
/// <remarks>Both routes are anonymous by nature: they are how a caller gets an identity in the first place.</remarks>
internal sealed class AuthEndpoints : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder auth = endpoints.MapGroup("/auth").WithTags("Authentication");

        auth.MapPost("/register", RegisterAsync)
            .ValidatingBody<RegisterRequest>()
            .WithName("Register")
            .WithSummary("Creates a customer account.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        auth.MapPost("/login", LoginAsync)
            .ValidatingBody<LoginRequest>()
            .WithName("Login")
            .WithSummary("Exchanges an email and a password for a bearer token.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<Created<RegisteredUserResponse>> RegisterAsync(
        RegisterRequest request,
        RegisterHandler handler,
        CancellationToken cancellationToken)
    {
        RegisteredUserResponse user = await handler.HandleAsync(
            new RegisterCommand(request.Email, request.Password, request.FirstName, request.LastName),
            cancellationToken);

        // No Location: an account is not a resource this API lets anyone read, so there is nothing to point at.
        return TypedResults.Created((string?)null, user);
    }

    private static async Task<Ok<AccessTokenResponse>> LoginAsync(
        LoginRequest request,
        LoginHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(new LoginCommand(request.Email, request.Password), cancellationToken));
}
