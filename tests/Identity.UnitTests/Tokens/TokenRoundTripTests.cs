using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Time.Testing;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.BuildingBlocks.Authentication;
using QubicaCinema.Identity.Api.Tokens;
using QubicaCinema.Identity.UnitTests.Fixtures;

namespace QubicaCinema.Identity.UnitTests.Tokens;

/// <summary>
/// The guarantee this chapter exists for: a token the real Identity token service signs is accepted by the
/// real bearer validation, and only for what its roles allow. The host is a few routes in memory; the
/// authentication, the policies and the current user are the ones every service registers.
/// </summary>
public sealed class TokenRoundTripTests : IAsyncDisposable
{
    private static readonly Guid CustomerId = Guid.Parse("0199a0c0-0000-7000-8000-0000000000c1");
    private static readonly Guid AdminId = Guid.Parse("0199a0c0-0000-7000-8000-0000000000a1");

    private readonly WebApplication _app;
    private readonly HttpClient _client;

    public TokenRoundTripTests()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = TestJwt.Issuer,
            ["Jwt:Audience"] = TestJwt.Audience,
            ["Jwt:SigningKey"] = TestJwt.SigningKey,
        });

        builder.Services.AddCinemaAuthentication(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.AddCinemaAuthorization();
        builder.Services.AddCinemaCurrentUser();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();

        _app.MapGet("/public", () => "open").AllowAnonymous();
        _app.MapGet("/authenticated", (ICurrentUser user) => new { user.UserId, user.IsAdministrator })
            .RequireAuthorization(CinemaPolicies.Authenticated);
        _app.MapGet("/customers", (ICurrentUser user) => user.UserId).RequireAuthorization(CinemaPolicies.Customer);
        _app.MapGet("/admins", (ICurrentUser user) => user.UserId).RequireAuthorization(CinemaPolicies.Admin);
        // A route that forgot RequireAuthorization but reads the caller anyway.
        _app.MapGet("/forgotten", (ICurrentUser user) => user.UserId).AllowAnonymous();

        _app.StartAsync().GetAwaiter().GetResult();
        _client = _app.GetTestClient();
    }

    [Fact]
    public async Task Should_let_anyone_reach_an_open_route()
    {
        (await Get("/public")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_answer_401_when_there_is_no_token()
    {
        (await Get("/authenticated")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_accept_a_customers_token_on_a_customer_route_and_read_who_they_are()
    {
        using HttpResponseMessage response = await Get("/authenticated", Token(CustomerId, CinemaRoles.Customer));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain(CustomerId.ToString(), Case.Insensitive);
        body.ShouldContain("\"isAdministrator\":false", Case.Insensitive);
        (await Get("/customers", Token(CustomerId, CinemaRoles.Customer))).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_answer_403_when_a_customers_token_meets_an_admin_route()
    {
        (await Get("/admins", Token(CustomerId, CinemaRoles.Customer))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_accept_an_admins_token_on_an_admin_route_and_flag_them_as_administrator()
    {
        (await Get("/admins", Token(AdminId, CinemaRoles.Admin))).StatusCode.ShouldBe(HttpStatusCode.OK);

        using HttpResponseMessage me = await Get("/authenticated", Token(AdminId, CinemaRoles.Admin));
        (await me.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("\"isAdministrator\":true", Case.Insensitive);
    }

    [Fact]
    public async Task Should_refuse_an_admin_the_customer_route()
    {
        // An administrator does not book for themselves: roles are not a hierarchy.
        (await Get("/customers", Token(AdminId, CinemaRoles.Admin))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_answer_401_for_a_token_signed_with_another_key()
    {
        JwtTokenService stranger = TestJwt.TokenService(
            TestJwt.Options(signingKey: "some-other-signing-key-that-is-long-enough-too"), Now());
        string token = stranger.CreateAccessToken(AdminId, "mallory@example.com", [CinemaRoles.Admin]).Value;

        (await Get("/admins", token)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_answer_401_for_a_token_meant_for_another_audience()
    {
        JwtTokenService other = TestJwt.TokenService(TestJwt.Options(audience: "some-other-api"), Now());
        string token = other.CreateAccessToken(AdminId, "ada@example.com", [CinemaRoles.Admin]).Value;

        (await Get("/admins", token)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_answer_401_once_the_token_has_expired()
    {
        // Validation reads the real clock, so the token is issued three hours ago with an hour of life.
        var past = new FakeTimeProvider(DateTimeOffset.UtcNow.AddHours(-3));
        string expired = TestJwt.TokenService(clock: past)
            .CreateAccessToken(AdminId, "ada@example.com", [CinemaRoles.Admin]).Value;

        (await Get("/admins", expired)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_fail_loudly_when_a_route_reads_the_caller_without_requiring_one()
    {
        await Should.ThrowAsync<InvalidOperationException>(() => Get("/forgotten"));
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    // Validation reads the real clock, so a token that should be accepted has to be issued "now" by it.
    private static FakeTimeProvider Now() => new(DateTimeOffset.UtcNow);

    private static string Token(Guid userId, string role) =>
        TestJwt.TokenService(clock: Now())
            .CreateAccessToken(userId, "ada@example.com", [role]).Value;

    private async Task<HttpResponseMessage> Get(string path, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
