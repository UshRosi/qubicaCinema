using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace QubicaCinema.BuildingBlocks.Authentication;

/// <summary>Configures bearer validation from the validated <see cref="JwtOptions"/>.</summary>
/// <remarks>
/// A class rather than an <c>AddJwtBearer(o => ...)</c> lambda, so the bearer handler reads the same validated
/// options the rest of the process does instead of re-reading <c>IConfiguration</c> on its own. It is also
/// what lets a test replace the whole configuration with one <c>Configure&lt;JwtOptions&gt;</c> call.
/// </remarks>
public sealed class ConfigureJwtBearerOptions(IOptionsMonitor<JwtOptions> jwt) : IConfigureNamedOptions<JwtBearerOptions>
{
    /// <summary>How far a clock may drift between the issuer and a validator before a token counts as expired.</summary>
    internal static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);

    /// <inheritdoc />
    public void Configure(string? name, JwtBearerOptions options)
    {
        // Other schemes would share this options type; only the bearer scheme is ours to configure.
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        JwtOptions settings = jwt.CurrentValue;

        // Off: "sub" stays "sub" and "role" stays "role", the names the issuer wrote. With it on, the handler
        // rewrites them into long .NET URIs and every reader has to know which spelling it will get.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            // Pinned, so a token cannot name a weaker algorithm (or none) and be believed.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = ClockSkew,
            NameClaimType = CinemaClaimTypes.Subject,
            RoleClaimType = CinemaClaimTypes.Role,
        };
    }
}
