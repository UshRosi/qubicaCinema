using Microsoft.Extensions.Options;
using QubicaCinema.Gateway.IntegrationTests.Fixtures;
using QubicaCinema.Gateway.RateLimiting;

namespace QubicaCinema.Gateway.IntegrationTests.RateLimiting;

/// <summary>The two places where a string or a number in configuration can be wrong and only a test notices.</summary>
public sealed class RateLimitConfigurationTests
{
    [Fact]
    public async Task The_booking_route_names_the_policy_the_code_registers()
    {
        await using var factory = new GatewayFactory();

        string? configured = factory.Services.GetRequiredService<IConfiguration>()
            ["ReverseProxy:Routes:booking-create:RateLimiterPolicy"];

        // A mismatch is not a compile error, and it is not silent either: YARP refuses to load a route that
        // names an unknown policy. This turns that runtime failure into a failing test.
        configured.ShouldBe(RateLimitPolicyNames.BookingCreation);
    }

    [Fact]
    public async Task A_budget_that_makes_no_sense_stops_the_gateway_from_starting()
    {
        await using var factory = new GatewayFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:PermitLimit"] = "10",
            ["RateLimiting:BookingCreationPermitLimit"] = "10",
        });

        Should.Throw<OptionsValidationException>(() => factory.CreateClient());
    }
}
