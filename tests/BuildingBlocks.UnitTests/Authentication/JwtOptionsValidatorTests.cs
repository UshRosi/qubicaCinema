using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QubicaCinema.BuildingBlocks.Authentication;

namespace QubicaCinema.BuildingBlocks.UnitTests.Authentication;

/// <summary>
/// A process with an unusable signing setup must refuse to start, with a sentence that names the fix, rather
/// than come up and answer 401 to everything.
/// </summary>
public sealed class JwtOptionsValidatorTests
{
    private const string GoodKey = "a-signing-key-that-is-comfortably-longer-than-32-bytes";

    [Fact]
    public void Should_accept_a_complete_configuration()
    {
        Validate(Options(), Environments.Production).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("too-short")]
    // Thirty-one characters: one byte under what HMAC-SHA256 needs.
    [InlineData("0123456789012345678901234567890")]
    public void Should_reject_a_key_shorter_than_32_bytes(string key)
    {
        ValidateOptionsResult result = Validate(Options(key: key), Environments.Development);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull().ShouldContain("at least 32 bytes");
    }

    [Fact]
    public void Should_leave_an_empty_key_to_the_Required_annotation()
    {
        // Reporting it twice, once as required and once as too short, would only add noise to the message.
        Validate(Options(key: string.Empty), Environments.Production).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Should_count_bytes_not_characters()
    {
        // Sixteen characters of two bytes each is exactly 32 bytes, which is enough.
        Validate(Options(key: new string('é', 16)), Environments.Production).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("/identity")]
    [InlineData("ftp://identity.example/")]
    public void Should_reject_an_issuer_that_is_not_an_absolute_uri(string issuer)
    {
        ValidateOptionsResult result = Validate(Options(issuer: issuer), Environments.Production);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull().ShouldContain("absolute http(s) URI");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Should_reject_the_development_placeholder_outside_development(string environment)
    {
        ValidateOptionsResult result = Validate(Options(key: JwtOptions.DevelopmentPlaceholderKey), environment);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull().ShouldContain("development placeholder");
        result.FailureMessage.ShouldNotBeNull().ShouldContain(environment);
    }

    [Fact]
    public void Should_accept_the_development_placeholder_in_development()
    {
        Validate(Options(key: JwtOptions.DevelopmentPlaceholderKey), Environments.Development).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Should_report_every_problem_at_once()
    {
        ValidateOptionsResult result = Validate(Options(key: "short", issuer: "nope"), Environments.Production);

        result.FailureMessage.ShouldNotBeNull().ShouldContain("at least 32 bytes");
        result.FailureMessage.ShouldNotBeNull().ShouldContain("absolute http(s) URI");
    }

    private static JwtOptions Options(string key = GoodKey, string issuer = "https://identity.example/") => new()
    {
        Issuer = issuer,
        Audience = "cinema-api",
        SigningKey = key,
    };

    private static ValidateOptionsResult Validate(JwtOptions options, string environment) =>
        new JwtOptionsValidator(new StubHostEnvironment(environment)).Validate(name: null, options);
}
