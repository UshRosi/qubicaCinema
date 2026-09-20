using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QubicaCinema.BuildingBlocks.Authentication;

namespace QubicaCinema.BuildingBlocks.UnitTests.Authentication;

/// <summary>What the bearer handler will enforce, read straight off the options it is given.</summary>
public sealed class ConfigureJwtBearerOptionsTests
{
    private const string Key = "a-signing-key-that-is-comfortably-longer-than-32-bytes";

    private readonly JwtBearerOptions _bearer = new();

    public ConfigureJwtBearerOptionsTests()
    {
        var jwt = new JwtOptions { Issuer = "https://identity.example/", Audience = "cinema-api", SigningKey = Key };

        new ConfigureJwtBearerOptions(new StaticOptionsMonitor<JwtOptions>(jwt))
            .Configure(JwtBearerDefaults.AuthenticationScheme, _bearer);
    }

    [Fact]
    public void Should_require_the_configured_issuer_and_audience()
    {
        _bearer.TokenValidationParameters.ValidateIssuer.ShouldBeTrue();
        _bearer.TokenValidationParameters.ValidIssuer.ShouldBe("https://identity.example/");
        _bearer.TokenValidationParameters.ValidateAudience.ShouldBeTrue();
        _bearer.TokenValidationParameters.ValidAudience.ShouldBe("cinema-api");
    }

    [Fact]
    public void Should_verify_the_signature_against_the_shared_key_and_only_with_HMAC_SHA256()
    {
        TokenValidationParameters parameters = _bearer.TokenValidationParameters;

        parameters.ValidateIssuerSigningKey.ShouldBeTrue();
        parameters.IssuerSigningKey.ShouldBeOfType<SymmetricSecurityKey>()
            .Key.ShouldBe(System.Text.Encoding.UTF8.GetBytes(Key));
        // Pinned, so a token cannot choose a weaker algorithm for itself.
        parameters.ValidAlgorithms.ShouldBe([SecurityAlgorithms.HmacSha256]);
    }

    [Fact]
    public void Should_reject_an_expired_token_after_thirty_seconds_of_clock_skew()
    {
        _bearer.TokenValidationParameters.ValidateLifetime.ShouldBeTrue();
        _bearer.TokenValidationParameters.RequireExpirationTime.ShouldBeTrue();
        _bearer.TokenValidationParameters.ClockSkew.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Should_read_the_claim_names_the_issuer_wrote()
    {
        _bearer.MapInboundClaims.ShouldBeFalse();
        _bearer.TokenValidationParameters.NameClaimType.ShouldBe("sub");
        _bearer.TokenValidationParameters.RoleClaimType.ShouldBe("role");
    }

    [Fact]
    public void Should_leave_another_scheme_alone()
    {
        var other = new JwtBearerOptions();
        var jwt = new JwtOptions { Issuer = "https://identity.example/", Audience = "cinema-api", SigningKey = Key };

        new ConfigureJwtBearerOptions(new StaticOptionsMonitor<JwtOptions>(jwt)).Configure("SomethingElse", other);

        other.TokenValidationParameters.ValidIssuer.ShouldBeNull();
    }
}
