using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace QubicaCinema.EndToEndTests.Fixtures;

/// <summary>What a successful login through the gateway returns.</summary>
internal sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, IReadOnlyList<string> Roles);

/// <summary>Registers and signs in through the gateway's own routes — never a token minted by the test.</summary>
/// <remarks>
/// This is the point of the end-to-end tier: the token every test authenticates with here is the one the
/// real Identity service issued, validated by whichever downstream service the gateway routes the call to.
/// </remarks>
internal static class IdentityFlows
{
    /// <summary>Logs in and returns a client that presents the token on every request.</summary>
    internal static async Task<HttpClient> LoginAsAsync(
        this HttpClient gateway, string email, string password, CancellationToken cancellationToken)
    {
        using HttpResponseMessage login = await gateway.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password }, cancellationToken);
        login.EnsureSuccessStatusCode();
        LoginResponse token = (await login.Content.ReadFromJsonAsync<LoginResponse>(TestJson.Options, cancellationToken))!;

        var client = new HttpClient { BaseAddress = gateway.BaseAddress };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        return client;
    }

    /// <summary>Registers a fresh customer and returns a client authenticated as them.</summary>
    internal static async Task<HttpClient> RegisterCustomerAsync(this HttpClient gateway, CancellationToken cancellationToken)
    {
        string email = $"{Guid.NewGuid():N}@e2e.tests";
        const string password = "Cinema!Test1";

        using HttpResponseMessage register = await gateway.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password, firstName = "Ada", lastName = "Lovelace" },
            cancellationToken);
        register.EnsureSuccessStatusCode();

        return await gateway.LoginAsAsync(email, password, cancellationToken);
    }
}
