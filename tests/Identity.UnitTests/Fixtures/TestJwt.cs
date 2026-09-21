using Microsoft.Extensions.Time.Testing;
using QubicaCinema.BuildingBlocks.Authentication;
using QubicaCinema.Identity.Api.Tokens;

namespace QubicaCinema.Identity.UnitTests.Fixtures;

/// <summary>One coherent signing setup, and the real token service built on it.</summary>
internal static class TestJwt
{
    internal const string Issuer = "https://identity.tests/";
    internal const string Audience = "cinema-tests-api";
    internal const string SigningKey = "identity-tests-signing-key-comfortably-long-enough";

    internal static readonly DateTimeOffset Start = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    internal static JwtOptions Options(
        string signingKey = SigningKey,
        string audience = Audience,
        int lifetimeMinutes = 60) => new()
    {
        Issuer = Issuer,
        Audience = audience,
        SigningKey = signingKey,
        AccessTokenLifetimeMinutes = lifetimeMinutes,
    };

    internal static JwtTokenService TokenService(JwtOptions? options = null, TimeProvider? clock = null) =>
        new(new StaticOptionsMonitor<JwtOptions>(options ?? Options()), clock ?? new FakeTimeProvider(Start));
}
