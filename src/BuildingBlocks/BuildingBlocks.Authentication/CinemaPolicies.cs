using QubicaCinema.BuildingBlocks.Application.Security;

namespace QubicaCinema.BuildingBlocks.Authentication;

/// <summary>The names of the authorization policies, used by endpoints and by the gateway's routes.</summary>
public static class CinemaPolicies
{
    /// <summary>The caller holds the <see cref="CinemaRoles.Admin"/> role.</summary>
    public const string Admin = "Admin";

    /// <summary>The caller holds the <see cref="CinemaRoles.Customer"/> role.</summary>
    public const string Customer = "Customer";

    /// <summary>
    /// The caller presented a valid token, whatever their role. Lower case because the gateway's routes name
    /// it in <c>appsettings.json</c>, where the spelling is part of the file's vocabulary.
    /// </summary>
    public const string Authenticated = "authenticated";
}
