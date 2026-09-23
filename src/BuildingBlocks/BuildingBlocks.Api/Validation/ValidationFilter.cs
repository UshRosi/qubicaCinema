using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using QubicaCinema.BuildingBlocks.Api.Errors;

namespace QubicaCinema.BuildingBlocks.Api.Validation;

/// <summary>
/// Validates one request body before the endpoint runs, and answers 422 when it does not hold up.
/// </summary>
/// <remarks>
/// An endpoint filter, not a second pipeline: it runs on the endpoints that ask for it, in the order they
/// declare, and it is visible in the route definition. Nothing is discovered by scanning, so a route
/// without this line is obviously unvalidated rather than mysteriously so.
/// <para>
/// 422 and not 400: the request parsed as JSON and reached the endpoint, so it is well formed but wrong.
/// 400 is reserved for what never became a request at all — malformed JSON, a discriminator in the wrong
/// place. The litmus test in the API design is "could a client tell from the payload alone?".
/// </para>
/// <para>
/// A body the endpoint declares optional (a nullable parameter) may be left out, and then there is nothing
/// to validate. Any other missing argument is a wiring mistake and fails loudly.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
public sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
    where TRequest : notnull
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.OfType<TRequest>().FirstOrDefault() is not { } request)
        {
            if (BodyIsOptional(context.HttpContext))
            {
                return await next(context);
            }

            // The endpoint does not take a TRequest: a wiring mistake, and one that would otherwise let
            // every request through unvalidated.
            throw new InvalidOperationException(
                $"No argument of type {typeof(TRequest).Name} was bound, so it cannot be validated.");
        }

        ValidationResult result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

        if (result.IsValid)
        {
            return await next(context);
        }

        return TypedResults.Problem(
            detail: "One or more fields are not acceptable.",
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Validation failed",
            type: ProblemTypes.ValidationFailed,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["errors"] = result.Errors
                    .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(failure => failure.ErrorMessage).ToArray(),
                        StringComparer.Ordinal),
            });
    }

    /// <summary>Whether the endpoint declares a <typeparamref name="TRequest"/> body that a client may leave out.</summary>
    private static bool BodyIsOptional(HttpContext httpContext) =>
        httpContext.GetEndpoint()?.Metadata.GetMetadata<IAcceptsMetadata>() is { IsOptional: true } accepts
        && accepts.RequestType == typeof(TRequest);
}
