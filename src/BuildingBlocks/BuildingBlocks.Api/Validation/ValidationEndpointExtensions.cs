using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace QubicaCinema.BuildingBlocks.Api.Validation;

/// <summary>Attaches request validation to a route.</summary>
public static class ValidationEndpointExtensions
{
    /// <summary>
    /// Validates the bound <typeparamref name="TRequest"/> before the endpoint runs, answering 422 if it
    /// does not hold up.
    /// </summary>
    /// <remarks>
    /// Documented as a validation problem, not a plain one: the filter's body is a problem document with a
    /// top-level <c>errors</c> map of field to messages, which is what that schema describes.
    /// </remarks>
    public static RouteHandlerBuilder ValidatingBody<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : notnull =>
        builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
}
