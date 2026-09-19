using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.BuildingBlocks.Api.Errors;

/// <summary>
/// Turns every <see cref="DomainException"/> into an RFC 9457 ProblemDetails response.
/// </summary>
/// <remarks>
/// The single reason no endpoint and no handler in this solution contains a <c>try/catch</c>. A use case
/// states what went wrong by throwing; deciding that "the seat is taken" is a 409 is an HTTP concern and
/// belongs here, once, where the whole mapping can be read at a glance and changed in one place.
/// <para>
/// Anything that is not a <see cref="DomainException"/> is deliberately left alone: it is a bug, it is
/// logged as one, and it becomes the framework's own 500 rather than being dressed up as a handled error.
/// </para>
/// </remarks>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        (int status, string type, string title) = Describe(domainException.Kind);

        // Expected outcomes, not faults: logged at Information so a wall of 404s cannot hide a real error.
        logger.LogInformation(
            "{Kind} on {Method} {Path}: {Message}",
            domainException.Kind,
            httpContext.Request.Method,
            httpContext.Request.Path,
            domainException.Message);

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = domainException,
            ProblemDetails =
            {
                Status = status,
                Type = type,
                Title = title,
                // The message is written by the model for a person to read; it never contains
                // infrastructure detail, so it is safe to return as-is.
                Detail = domainException.Message,
                Extensions = domainException.Extensions.ToDictionary(
                    extension => extension.Key,
                    extension => extension.Value),
            },
        });
    }

    private static (int Status, string Type, string Title) Describe(DomainErrorKind kind) => kind switch
    {
        DomainErrorKind.NotFound => (StatusCodes.Status404NotFound, ProblemTypes.NotFound, "Not found"),
        DomainErrorKind.Conflict => (StatusCodes.Status409Conflict, ProblemTypes.Conflict, "Conflict"),
        DomainErrorKind.Invalid => (StatusCodes.Status422UnprocessableEntity, ProblemTypes.Invalid, "Unprocessable request"),
        DomainErrorKind.Forbidden => (StatusCodes.Status403Forbidden, ProblemTypes.Forbidden, "Forbidden"),
        DomainErrorKind.PreconditionFailed => (StatusCodes.Status412PreconditionFailed, ProblemTypes.PreconditionFailed, "Precondition failed"),

        // Unreachable while the enum and this switch agree; it exists so that adding a kind and forgetting
        // this method is a wrong status code in one test, not a 500 in production.
        _ => (StatusCodes.Status500InternalServerError, ProblemTypes.Unexpected, "Unexpected error"),
    };
}
