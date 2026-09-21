namespace QubicaCinema.BuildingBlocks.Application.Security;

/// <summary>The roles a user can hold. One place for the strings, so the seed, the token and the policies agree.</summary>
public static class CinemaRoles
{
    /// <summary>Acts for the cinema: manages the catalogue and may see any booking.</summary>
    public const string Admin = "Admin";

    /// <summary>A member of the public who books seats for themselves.</summary>
    public const string Customer = "Customer";
}
