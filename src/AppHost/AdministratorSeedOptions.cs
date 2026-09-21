namespace QubicaCinema.AppHost;

/// <summary>
/// The administrator the Identity migration creates, bound from the <c>IdentitySeed</c> configuration section.
/// </summary>
/// <remarks>
/// Read by the AppHost only to be handed on: the migration service receives these as
/// <c>IdentitySeed__AdministratorEmail</c> and <c>IdentitySeed__AdministratorPassword</c>, the same plain keys
/// it reads under any other deployment.
/// </remarks>
internal sealed record AdministratorSeedOptions
{
    internal const string SectionName = "IdentitySeed";

    /// <summary>The administrator's email address, which is also their login.</summary>
    public string AdministratorEmail { get; init; } = string.Empty;

    /// <summary>The administrator's password.</summary>
    public string AdministratorPassword { get; init; } = string.Empty;
}
