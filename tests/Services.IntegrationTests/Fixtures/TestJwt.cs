using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>
/// The one signing key all three hosts in this tier validate against, and the way a test mints a token for
/// a user without going through Identity's own <c>/auth/login</c>.
/// </summary>
/// <remarks>
/// Mirrors <c>tests/Gateway.IntegrationTests/Fixtures/TestTokens.cs</c>, duplicated rather than shared: this
/// solution has no common test project, on purpose (see the other test assemblies). Bookings and Catalog
/// both validate the token themselves — the gateway is not in this tier — so every host must be configured
/// with exactly this issuer, audience and key, not the placeholder from <c>appsettings.Development.json</c>.
/// </remarks>
internal static class TestJwt
{
    internal const string Issuer = "https://services.tests/identity";
    internal const string Audience = "services-tests-api";
    internal const string SigningKey = "services-integration-tests-signing-key-long-enough-for-hmac";

    /// <summary>A token for the user, carrying one claim per role, signed with <see cref="SigningKey"/>.</summary>
    internal static string For(Guid userId, params string[] roles)
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [new("sub", userId.ToString()), .. roles.Select(role => new Claim("role", role))];

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = Issuer,
            Audience = Audience,
            NotBefore = now.AddMinutes(-1),
            IssuedAt = now.AddMinutes(-1),
            Expires = now.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256),
        });
    }
}
