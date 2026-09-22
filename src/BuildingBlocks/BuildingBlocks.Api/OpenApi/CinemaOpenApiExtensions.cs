using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace QubicaCinema.BuildingBlocks.Api.OpenApi;

/// <summary>
/// Publishes a service's OpenAPI document, the same way in every service.
/// </summary>
public static class CinemaOpenApiExtensions
{
    /// <summary>
    /// Registers one document and everything that makes it describe this solution's conventions.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="documentName">One of <see cref="CinemaApiDocuments"/>.</param>
    /// <param name="title">The name the reference page shows for the document.</param>
    /// <param name="description">What the service is for, in a sentence or two.</param>
    public static IServiceCollection AddCinemaOpenApi(
        this IServiceCollection services,
        string documentName,
        string title,
        string description)
    {
        services.AddOpenApi(documentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo { Title = title, Version = "v1", Description = description };

                return Task.CompletedTask;
            });

            // The generator fills servers in with the address the request arrived on, which for a request
            // proxied by the gateway is the service's own. A "Try it" call would then skip the gateway, and
            // with it the rate limit and the first check of the token. With no servers listed the document
            // is relative to wherever it was fetched from, and that is the gateway.
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Servers = [];

                return Task.CompletedTask;
            });

            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<AuthorizationSecurityTransformer>();
        });

        return services;
    }

    /// <summary>
    /// Maps <c>/openapi/{documentName}.json</c> when the process is allowed to publish it.
    /// </summary>
    /// <remarks>
    /// Anonymous and outside the rate limiter, like the health endpoints: the reference page fetches it on
    /// every load, and a schema is not something a caller should have to be signed in to read.
    /// </remarks>
    public static WebApplication MapCinemaOpenApi(this WebApplication app)
    {
        if (!OpenApiEndpointOptions.ShouldExpose(app.Environment, app.Configuration))
        {
            return app;
        }

        app.MapOpenApi("/openapi/{documentName}.json")
            .AllowAnonymous()
            .DisableRateLimiting();

        return app;
    }
}
