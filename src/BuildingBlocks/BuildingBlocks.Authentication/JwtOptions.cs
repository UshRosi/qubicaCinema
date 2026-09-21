using System.ComponentModel.DataAnnotations;

namespace QubicaCinema.BuildingBlocks.Authentication;

/// <summary>What the issuer signs with and what every validator checks against. Identical in every process.</summary>
/// <remarks>
/// The AppHost hands the same three values to Identity, Catalog, Booking and the gateway, as
/// <c>Jwt__Issuer</c>, <c>Jwt__Audience</c> and <c>Jwt__SigningKey</c>. A mismatch anywhere is a 401 that says
/// nothing about why, so the shape is validated when the process starts rather than when the first token fails.
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The development key, committed in the AppHost's <c>appsettings.json</c> so a clone runs without setup.
    /// <see cref="JwtOptionsValidator"/> refuses it in every other environment.
    /// </summary>
    public const string DevelopmentPlaceholderKey = "qubica-cinema-local-development-signing-key-32b+";

    /// <summary>Who issues the tokens, as an absolute http(s) URI. Written into <c>iss</c>.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Who the tokens are for. Written into <c>aud</c>; a token meant for another API is rejected.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>The shared HMAC-SHA256 secret. At least 32 bytes; see <see cref="JwtOptionsValidator"/>.</summary>
    [Required(AllowEmptyStrings = false)]
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>How long an access token is good for.</summary>
    [Range(1, 24 * 60)]
    public int AccessTokenLifetimeMinutes { get; init; } = 60;
}
