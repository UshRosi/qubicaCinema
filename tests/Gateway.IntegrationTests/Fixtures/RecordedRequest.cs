namespace QubicaCinema.Gateway.IntegrationTests.Fixtures;

/// <summary>A request as the stubbed service received it: enough to say where it went and what came along.</summary>
/// <param name="Method">The HTTP method the gateway used.</param>
/// <param name="Uri">The full destination URI, whose host says which service was chosen.</param>
/// <param name="Headers">Request and content headers, by name, case-insensitively; values joined by a comma.</param>
internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers)
{
    internal static RecordedRequest From(HttpRequestMessage request)
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);

        foreach (var header in request.Headers.Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()))
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        return new RecordedRequest(request.Method, request.RequestUri!, headers);
    }

    /// <summary>The value of a header, or <see langword="null"/> when the request did not carry it.</summary>
    internal string? Header(string name) => Headers.GetValueOrDefault(name);
}
