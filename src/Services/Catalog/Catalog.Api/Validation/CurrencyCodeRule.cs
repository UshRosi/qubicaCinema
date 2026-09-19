using FluentValidation;

namespace QubicaCinema.Catalog.Api.Validation;

/// <summary>The currency-code rule, shared by every request that carries a price.</summary>
internal static class CurrencyCodeRule
{
    /// <summary>
    /// Requires a three-letter ISO-4217 code such as <c>EUR</c>. The same shape <c>Money</c> accepts, checked
    /// here so the caller hears about it alongside every other field instead of one exception at a time.
    /// </summary>
    internal static IRuleBuilderOptions<T, string> MustBeCurrencyCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(code => code is { Length: 3 } && code.All(char.IsAsciiLetter))
            .WithMessage("'{PropertyName}' must be a three-letter ISO-4217 code, such as EUR.");
}
