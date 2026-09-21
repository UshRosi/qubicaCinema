using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using QubicaCinema.BuildingBlocks.Authentication;

namespace QubicaCinema.Identity.Api.Tokens;

/// <summary>Signs HMAC-SHA256 tokens with the key every validator in the solution shares.</summary>
internal sealed class JwtTokenService : ITokenService
{
    private static readonly JsonWebTokenHandler Handler = new();

    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;

    // Built once: deriving the key and the credentials is the only expensive part of signing, and it is the
    // main reason this class is a singleton and not transient. A key rotation therefore needs a restart, which
    // is the same for the validators, so nothing is left half-rotated.
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptionsMonitor<JwtOptions> options, TimeProvider clock)
    {
        _options = options.CurrentValue;
        _clock = clock;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    /// <inheritdoc />
    public AccessToken CreateAccessToken(Guid userId, string email, IReadOnlyCollection<string> roles)
    {
        DateTimeOffset issuedAt = _clock.GetUtcNow();
        DateTimeOffset expiresAt = issuedAt.AddMinutes(_options.AccessTokenLifetimeMinutes);

        List<Claim> claims =
        [
            new(CinemaClaimTypes.Subject, userId.ToString()),
            new(CinemaClaimTypes.Email, email),
            // A unique id per token, so an individual one can be named in a log or a revocation list later.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            .. roles.Select(role => new Claim(CinemaClaimTypes.Role, role)),
        ];

        string token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _credentials,
        });

        return new AccessToken(token, expiresAt);
    }
}
