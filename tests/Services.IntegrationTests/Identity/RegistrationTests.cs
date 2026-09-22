using System.Net;
using System.Text.Json.Nodes;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Identity;

/// <summary>Proves registration and login against the real Identity database and the real password hashing.</summary>
public sealed class RegistrationTests(CinemaFixture cinema)
{
    [Fact]
    public async Task A_registered_customer_can_log_in_and_the_token_carries_the_Customer_role()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = cinema.Identity.CreateClient();
        string email = $"{Guid.NewGuid():N}@services.tests";

        using HttpResponseMessage register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password = "Cinema!Test1", firstName = "Ada", lastName = "Lovelace" },
            cancellationToken);
        register.StatusCode.ShouldBe(HttpStatusCode.Created);

        using HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = "Cinema!Test1" }, cancellationToken);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);

        AccessTokenResponse token = (await login.Content.ReadFromJsonAsync<AccessTokenResponse>(
            TestJson.Options, cancellationToken))!;
        token.Roles.ShouldContain("Customer");
        token.AccessToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task The_same_email_twice_is_rejected()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = cinema.Identity.CreateClient();
        string email = $"{Guid.NewGuid():N}@services.tests";
        var body = new { email, password = "Cinema!Test1", firstName = "Ada", lastName = "Lovelace" };

        using HttpResponseMessage first = await client.PostAsJsonAsync("/api/v1/auth/register", body, cancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        using HttpResponseMessage second = await client.PostAsJsonAsync("/api/v1/auth/register", body, cancellationToken);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_wrong_password_and_an_unknown_account_get_the_same_answer()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = cinema.Identity.CreateClient();
        string email = $"{Guid.NewGuid():N}@services.tests";

        using HttpResponseMessage register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password = "Cinema!Test1", firstName = "Ada", lastName = "Lovelace" },
            cancellationToken);
        register.StatusCode.ShouldBe(HttpStatusCode.Created);

        using HttpResponseMessage wrongPassword = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = "NotItAtAll1!" }, cancellationToken);

        using HttpResponseMessage unknownAccount = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email = $"{Guid.NewGuid():N}@services.tests", password = "Whatever1!" }, cancellationToken);

        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknownAccount.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Every field but traceId, which ProblemDetails stamps with the current request's own trace and so
        // can never match between two different calls, must read identically: the point of the test.
        JsonNode wrongPasswordBody = await ProblemWithoutTraceIdAsync(wrongPassword, cancellationToken);
        JsonNode unknownAccountBody = await ProblemWithoutTraceIdAsync(unknownAccount, cancellationToken);
        wrongPasswordBody.ToJsonString().ShouldBe(unknownAccountBody.ToJsonString());
    }

    private static async Task<JsonNode> ProblemWithoutTraceIdAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        JsonNode problem = JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken))!;
        problem.AsObject().Remove("traceId");

        return problem;
    }

    private sealed record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt, IReadOnlyList<string> Roles);
}
