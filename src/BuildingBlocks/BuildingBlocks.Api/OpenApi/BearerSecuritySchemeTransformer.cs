using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace QubicaCinema.BuildingBlocks.Api.OpenApi;

/// <summary>
/// Declares the bearer-token security scheme, which is what gives the reference page its authorize button.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    /// <summary>The name operations refer to when they say they need a token.</summary>
    public const string SchemeName = "Bearer";

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "The accessToken returned by POST /api/v1/auth/login.",
        };

        return Task.CompletedTask;
    }
}
