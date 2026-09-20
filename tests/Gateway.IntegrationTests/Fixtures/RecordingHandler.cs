using System.Net;
using System.Text;

namespace QubicaCinema.Gateway.IntegrationTests.Fixtures;

/// <summary>
/// Stands in for both services: writes down every request the gateway forwards and answers with whatever
/// <see cref="Respond"/> says.
/// </summary>
internal sealed class RecordingHandler : HttpMessageHandler
{
    private readonly List<RecordedRequest> _requests = [];
    private readonly Lock _gate = new();

    /// <summary>What the stubbed service answers. Replace it to simulate a failing or a specific service.</summary>
    internal Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = OkWithTheHeadersAServiceSets;

    /// <summary>Everything forwarded so far, in arrival order.</summary>
    internal IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_gate)
            {
                return [.. _requests];
            }
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _requests.Add(RecordedRequest.From(request));
        }

        return Task.FromResult(Respond(request));
    }

    /// <summary>The three response headers the services rely on and the gateway must not disturb.</summary>
    private static HttpResponseMessage OkWithTheHeadersAServiceSets(HttpRequestMessage request)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };

        response.Headers.TryAddWithoutValidation("ETag", "\"v1\"");
        response.Headers.TryAddWithoutValidation("Cache-Control", "no-store");
        response.Headers.TryAddWithoutValidation("Location", "/api/v1/bookings/abc");

        return response;
    }
}
