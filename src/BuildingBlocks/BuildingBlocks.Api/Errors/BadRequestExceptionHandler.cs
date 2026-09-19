using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace QubicaCinema.BuildingBlocks.Api.Errors;

/// <summary>
/// Answers a request the framework could not bind — malformed JSON, an unknown discriminator — with 400
/// ProblemDetails instead of a 500.
/// </summary>
/// <remarks>
/// Minimal APIs throw <see cref="BadHttpRequestException"/> for such a body whenever
/// <c>RouteHandlerOptions.ThrowOnBadRequest</c> is on, which it is in Development. The exception already
/// carries the right status code, but nothing reads it: <c>UseExceptionHandler</c> sees an unhandled
/// exception and answers 500, telling a client that sent garbage that the server is broken. This handler
/// is registered before <see cref="DomainExceptionHandler"/> and simply honours the status code.
/// <para>
/// One case does not arrive as a <see cref="BadHttpRequestException"/> at all: a polymorphic body with no
/// discriminator makes System.Text.Json throw <see cref="NotSupportedException"/> ("must specify a type
/// discriminator") instead of a <c>JsonException</c>, and minimal APIs convert only the latter. So a
/// <see cref="NotSupportedException"/> raised by System.Text.Json while the response has not started is
/// treated as the client's malformed body too. That is safe here because no response contract is an
/// abstract polymorphic type — the serializer can only hit this while reading a request.
/// </para>
/// <para>
/// The detail is fixed text, not the exception message: the message describes the parser's internals and
/// can echo the request back, and "the body is not a valid request" is all a client needs.
/// </para>
/// </remarks>
public sealed class BadRequestExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private const string SerializerAssembly = "System.Text.Json";

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int? statusCode = exception switch
        {
            BadHttpRequestException badRequest => badRequest.StatusCode,
            NotSupportedException { Source: SerializerAssembly } when !httpContext.Response.HasStarted =>
                StatusCodes.Status400BadRequest,
            _ => null,
        };

        if (statusCode is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = statusCode,
                Type = ProblemTypes.MalformedRequest,
                Title = "Malformed request",
                Detail = "The request could not be read. Check that the body is valid JSON in the documented shape.",
            },
        });
    }
}
