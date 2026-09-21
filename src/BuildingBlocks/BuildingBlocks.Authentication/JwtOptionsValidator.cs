using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace QubicaCinema.BuildingBlocks.Authentication;

/// <summary>The rules for <see cref="JwtOptions"/> that data annotations cannot express.</summary>
public sealed class JwtOptionsValidator(IHostEnvironment environment) : IValidateOptions<JwtOptions>
{
    /// <summary>HMAC-SHA256 refuses a key shorter than its 256-bit block, so anything under this is a startup error.</summary>
    internal const int MinimumKeyBytes = 32;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        List<string> failures = [];

        // An empty key is already reported by [Required]; a second, less useful message would only add noise.
        if (options.SigningKey.Length > 0 && Encoding.UTF8.GetByteCount(options.SigningKey) < MinimumKeyBytes)
        {
            failures.Add(
                $"{nameof(JwtOptions.SigningKey)} must be at least {MinimumKeyBytes} bytes long for HMAC-SHA256. " +
                "Set Jwt__SigningKey to a longer secret.");
        }

        // The scheme is checked as well: on Linux "/identity" is an absolute URI too (file:///identity), and an
        // issuer that is a file path is a typo, not a choice.
        if (options.Issuer.Length > 0 && !IsAbsoluteWebUri(options.Issuer))
        {
            failures.Add($"{nameof(JwtOptions.Issuer)} must be an absolute http(s) URI, for example https://identity.example/.");
        }

        // The placeholder is public: it is in the repository. Accepting it anywhere that is not a developer's
        // machine would let anyone who has read the repository mint an administrator's token.
        if (!environment.IsDevelopment() && options.SigningKey == JwtOptions.DevelopmentPlaceholderKey)
        {
            failures.Add(
                $"{nameof(JwtOptions.SigningKey)} is the development placeholder, which is published in the " +
                $"repository, and this is the {environment.EnvironmentName} environment. Supply a real secret.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsAbsoluteWebUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
