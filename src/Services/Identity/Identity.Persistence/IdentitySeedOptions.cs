using System.ComponentModel.DataAnnotations;

namespace QubicaCinema.Identity.Persistence;

/// <summary>The administrator that exists from the first start, so somebody can manage the catalogue.</summary>
/// <remarks>
/// Set by the AppHost and documented in the README: a reviewer needs a token before any write can be tried.
/// These are local development credentials, not secrets, and a real deployment overrides every one.
/// </remarks>
public sealed class IdentitySeedOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "IdentitySeed";

    /// <summary>The administrator's email address, which is also their login.</summary>
    [Required, EmailAddress]
    public string AdministratorEmail { get; init; } = string.Empty;

    /// <summary>The administrator's password. Must satisfy the password rules Identity is configured with.</summary>
    [Required]
    public string AdministratorPassword { get; init; } = string.Empty;

    /// <summary>The administrator's given name.</summary>
    [Required]
    public string AdministratorFirstName { get; init; } = "Cinema";

    /// <summary>The administrator's family name.</summary>
    [Required]
    public string AdministratorLastName { get; init; } = "Administrator";
}
