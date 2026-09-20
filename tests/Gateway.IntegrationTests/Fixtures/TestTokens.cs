using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace QubicaCinema.Gateway.IntegrationTests.Fixtures;

/// <summary>
/// Mints bearer tokens the way Identity does, without Identity: the tests exercise the gateway's validation,
/// so the issuer here is deliberately not the production one.
/// </summary>
internal static class TestTokens
{
    internal const string Issuer = "https://gateway.tests/identity";
    internal const string Audience = "gateway-tests-api";
    internal const string SigningKey = "gateway-tests-signing-key-long-enough-for-hmac-sha256";

    /// <summary>A token for the user, carrying the roles, signed with the key the factory configures unless told otherwise.</summary>
    internal static string For(
        Guid userId,
        IEnumerable<string> roles,
        string signingKey = SigningKey,
        TimeSpan? lifetime = null)
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [new("sub", userId.ToString()), .. roles.Select(role => new Claim("role", role))];

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = Issuer,
            Audience = Audience,
            // An expired token needs its start moved back too: a token that ends before it begins is refused
            // when it is minted, and what the test wants is one that was fine and has since lapsed.
            NotBefore = now.AddHours(-2),
            IssuedAt = now.AddHours(-2),
            Expires = now + (lifetime ?? TimeSpan.FromHours(1)),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256),
        });
    }
}
