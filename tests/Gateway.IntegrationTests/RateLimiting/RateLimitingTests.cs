using System.Net;
using QubicaCinema.BuildingBlocks.Api.Errors;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Gateway.IntegrationTests.Errors;
using QubicaCinema.Gateway.IntegrationTests.Fixtures;

namespace QubicaCinema.Gateway.IntegrationTests.RateLimiting;

/// <summary>The limits are small here so a test can exhaust them; the production values are in appsettings.</summary>
public sealed class RateLimitingTests
{
    private static readonly Dictionary<string, string?> SmallBudgets = new()
    {
        ["RateLimiting:PermitLimit"] = "3",
        ["RateLimiting:BookingCreationPermitLimit"] = "2",
    };

    [Fact]
    public async Task A_client_over_its_budget_gets_a_429_with_Retry_After_and_a_problem_body()
    {
        await using var factory = new GatewayFactory(SmallBudgets);
        using var client = factory.CreateClient();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            using var allowed = await client.GetAsync("/api/v1/movies", TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var refused = await client.GetAsync("/api/v1/movies", TestContext.Current.CancellationToken);

        refused.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        refused.Headers.RetryAfter.ShouldNotBeNull().Delta.ShouldNotBeNull().TotalSeconds.ShouldBeGreaterThanOrEqualTo(1);
        refused.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await GatewayErrorTests.ReadType(refused)).ShouldBe(ProblemTypes.TooManyRequests);
        // The refused request never reached a service.
        factory.Downstream.Requests.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Creating_bookings_has_a_stricter_budget_than_reading_them()
    {
        await using var factory = new GatewayFactory(new Dictionary<string, string?>(SmallBudgets) { ["RateLimiting:PermitLimit"] = "50" });
        using var client = factory.CreateClientFor(TestIds.Customer, CinemaRoles.Customer);

        for (int attempt = 0; attempt < 2; attempt++)
        {
            using var created = await client.PostAsync("/api/v1/bookings", content: null, TestContext.Current.CancellationToken);
            created.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var refused = await client.PostAsync("/api/v1/bookings", content: null, TestContext.Current.CancellationToken);
        using var read = await client.GetAsync("/api/v1/bookings", TestContext.Current.CancellationToken);

        refused.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        // Same path, other method: the named policy is on the POST route only.
        read.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Two_accounts_behind_one_address_each_get_their_own_budget()
    {
        await using var factory = new GatewayFactory(SmallBudgets);
        using var first = factory.CreateClientFor(TestIds.Customer, CinemaRoles.Customer);
        using var second = factory.CreateClientFor(TestIds.OtherCustomer, CinemaRoles.Customer);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            using var allowed = await first.GetAsync("/api/v1/bookings", TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var firstRefused = await first.GetAsync("/api/v1/bookings", TestContext.Current.CancellationToken);
        // Same test server, so the same remote address; only the token tells the two callers apart.
        using var secondAllowed = await second.GetAsync("/api/v1/bookings", TestContext.Current.CancellationToken);

        firstRefused.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        secondAllowed.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
