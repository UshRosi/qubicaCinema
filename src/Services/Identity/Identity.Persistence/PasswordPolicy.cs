namespace QubicaCinema.Identity.Persistence;

/// <summary>The password rules that can be stated as numbers, so registration can check them before Identity does.</summary>
public static class PasswordPolicy
{
    /// <summary>The shortest password Identity accepts. The register request's validator reads this too, so the two cannot disagree.</summary>
    public const int MinimumLength = 8;
}
