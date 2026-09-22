using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace QubicaCinema.BuildingBlocks.Api.OpenApi;

/// <summary>
/// Marks an operation as needing a bearer token when its endpoint requires authorization.
/// </summary>
/// <remarks>
/// Derived from the endpoint's own authorization metadata, so the document cannot disagree with what the
/// service enforces and no route has to repeat itself. It also keeps the OpenAPI package out of
/// <c>BuildingBlocks.Authentication</c>, which only knows about policies.
/// <para>
/// The requirement's scheme reference must carry the document it belongs to. Without it the reference
/// cannot be resolved when the document is written, and it is serialised as an empty requirement
/// (<c>[{}]</c>) — which OpenAPI defines as "anonymous access is allowed", the opposite of what was meant.
/// </para>
/// </remarks>
public sealed class AuthorizationSecurityTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        IList<object> metadata = context.Description.ActionDescriptor.EndpointMetadata;

        bool requiresToken = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
        if (!requiresToken)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(BearerSecuritySchemeTransformer.SchemeName, context.Document)] = [],
        });

        return Task.CompletedTask;
    }
}
