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
    public static RouteHandlerBuilder ValidatingBody<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : notnull =>
        builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
