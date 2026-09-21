using Microsoft.IdentityModel.JsonWebTokens;

namespace QubicaCinema.BuildingBlocks.Authentication;

/// <summary>The claim names a token carries. The short JWT names, exactly as they appear on the wire.</summary>
/// <remarks>
/// Bearer validation is configured not to rewrite them into the long .NET URIs (<c>MapInboundClaims</c> off),
/// so the name the issuer wrote is the name a validator reads. Nothing is translated on the way.
/// </remarks>
public static class CinemaClaimTypes
{
    /// <summary>The user id, as the Identity service issued it.</summary>
    public const string Subject = JwtRegisteredClaimNames.Sub;

    /// <summary>The user's email address.</summary>
    public const string Email = JwtRegisteredClaimNames.Email;

    /// <summary>One claim per role the user holds.</summary>
    public const string Role = "role";
}
