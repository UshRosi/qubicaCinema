using QubicaCinema.BuildingBlocks.Api.OpenApi;
using Scalar.AspNetCore;

namespace QubicaCinema.Gateway.Documentation;

/// <summary>
/// The one page that documents the three services, served by the gateway so that a reviewer needs a single
/// address.
/// </summary>
/// <remarks>
/// The gateway does not build any of the documents. Each service publishes its own, and YARP proxies them
/// at the same path, so the page fetches <c>/openapi/catalog.json</c> from the gateway exactly as it would
/// from the service. The documents carry no server address, which is what makes "Try it" send its requests
/// back through the gateway, with its rate limit and its check of the token.
/// <para>
/// Mounted at <c>/docs</c> and not at <c>/</c>: an unrouted path is a ProblemDetails 404 here, and a
/// browser landing on the root is better told so than shown a page it did not ask for.
/// </para>
/// </remarks>
internal static class ApiReference
{
    private const string Path = "/docs";

    /// <summary>Maps the reference page when the process is allowed to publish documentation.</summary>
    internal static WebApplication MapApiReference(this WebApplication app)
    {
        if (!OpenApiEndpointOptions.ShouldExpose(app.Environment, app.Configuration))
        {
            return app;
        }

        app.MapScalarApiReference(Path, options => options
                .WithTitle("QubicaCinema API")
                .AddDocuments(CinemaApiDocuments.All)
                .AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.SchemeName)
                // Keeps the token across a page refresh, so a reviewer logs in once per session.
                .EnablePersistentAuthentication())
            // The gateway has no fallback policy, so this only states the intent: the page is public.
            .AllowAnonymous()
            // The page and its script bundle are several requests, and a reviewer's clicking around must
            // not spend the budget the limiter keeps for the API.
            .DisableRateLimiting();

        return app;
    }
}
