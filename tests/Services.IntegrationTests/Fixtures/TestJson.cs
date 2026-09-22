using System.Text.Json;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>
/// The one <see cref="JsonSerializerOptions"/> every test in this assembly reads a response with — the web
/// defaults, camelCase and case-insensitive, matching what every service in this solution writes.
/// </summary>
internal static class TestJson
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
