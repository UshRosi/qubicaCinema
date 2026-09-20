using Microsoft.AspNetCore.Http;
using QubicaCinema.BuildingBlocks.Api.Errors;

namespace QubicaCinema.BuildingBlocks.Authentication;

/// <summary>Gives the 401 and the 403 the same RFC 9457 vocabulary as every other error.</summary>
/// <remarks>
/// The authentication and authorization middleware answer with an empty body. With status-code pages enabled
/// the framework fills one in, and this puts the solution's own <c>type</c> URI on it, so a client branches on
/// one vocabulary whether the refusal came from the gateway or from a service.
/// </remarks>
public static class AuthenticationProblemDetails
{
    /// <summary>Assigned to <c>ProblemDetailsOptions.CustomizeProblemDetails</c>.</summary>
    public static void Describe(ProblemDetailsContext context)
    {
        switch (context.HttpContext.Response.StatusCode)
        {
            case StatusCodes.Status401Unauthorized:
                context.ProblemDetails.Type = ProblemTypes.Unauthenticated;
                context.ProblemDetails.Title = "Authentication required";
                context.ProblemDetails.Detail = "Send a valid bearer token. POST /api/v1/auth/login issues one.";
                break;

            case StatusCodes.Status403Forbidden:
                context.ProblemDetails.Type = ProblemTypes.Forbidden;
                context.ProblemDetails.Title = "Forbidden";
                context.ProblemDetails.Detail = "The token is valid, but its role may not do this.";
                break;
        }
    }
}
