using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using QubicaCinema.BuildingBlocks.Application.Security;
using QubicaCinema.Identity.Api.Tokens;
using QubicaCinema.Identity.UnitTests.Fixtures;

namespace QubicaCinema.Identity.UnitTests.Tokens;

/// <summary>What ends up inside the token: the claims a validator reads and the moment it stops working.</summary>
public sealed class JwtTokenServiceTests
{
    private static readonly Guid UserId = Guid.Parse("0199a0c0-0000-7000-8000-0000000000c1");

    private readonly FakeTimeProvider _clock = new(TestJwt.Start);

    [Fact]
    public void Should_carry_the_subject_the_email_and_every_role()
    {
        JsonWebToken token = Read(Create(roles: [CinemaRoles.Admin, CinemaRoles.Customer]));

        token.Subject.ShouldBe(UserId.ToString());
        token.GetClaim("email").Value.ShouldBe("ada@example.com");
        token.Claims.Where(claim => claim.Type == "role").Select(claim => claim.Value)
            .ShouldBe([CinemaRoles.Admin, CinemaRoles.Customer], ignoreOrder: true);
    }

    [Fact]
    public void Should_name_the_configured_issuer_and_audience()
    {
        JsonWebToken token = Read(Create());

        token.Issuer.ShouldBe(TestJwt.Issuer);
        token.Audiences.ShouldBe([TestJwt.Audience]);
    }

    [Fact]
    public void Should_expire_exactly_the_configured_lifetime_after_it_is_issued()
    {
        AccessToken token = Service(lifetimeMinutes: 45).CreateAccessToken(UserId, "ada@example.com", [CinemaRoles.Customer]);

        token.ExpiresAt.ShouldBe(TestJwt.Start.AddMinutes(45));
        Read(token).ValidTo.ShouldBe(TestJwt.Start.AddMinutes(45).UtcDateTime);
    }

    [Fact]
    public void Should_read_the_clock_it_is_given_not_the_wall_clock()
    {
        _clock.Advance(TimeSpan.FromDays(3));

        AccessToken token = Create(out _);

        token.ExpiresAt.ShouldBe(TestJwt.Start.AddDays(3).AddMinutes(60));
    }

    [Fact]
    public void Should_give_every_token_its_own_id()
    {
        JsonWebToken first = Read(Create());
        JsonWebToken second = Read(Create());

        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    public void Should_sign_with_HMAC_SHA256()
    {
        Read(Create()).Alg.ShouldBe("HS256");
    }

    private AccessToken Create(IReadOnlyCollection<string>? roles = null) =>
        Service().CreateAccessToken(UserId, "ada@example.com", roles ?? [CinemaRoles.Customer]);

    private AccessToken Create(out ITokenService service)
    {
        service = Service();

        return service.CreateAccessToken(UserId, "ada@example.com", [CinemaRoles.Customer]);
    }

    private static JsonWebToken Read(AccessToken token) => new(token.Value);

    private JwtTokenService Service(int lifetimeMinutes = 60) =>
        TestJwt.TokenService(TestJwt.Options(lifetimeMinutes: lifetimeMinutes), _clock);
}
