using System.Globalization;

namespace QubicaCinema.BuildingBlocks.Domain.ValueObjects;

/// <summary>
/// An amount of money in one currency.
/// </summary>
/// <remarks>
/// A shared kernel: Catalog prices a seat with it and Booking totals a purchase with it, and the two must
/// agree on what an amount of money is down to the rounding. It is deliberately the only kind of type
/// shared between the services — no entity, no aggregate and no rule about cinemas lives here — so the
/// coupling is to a stable definition, not to another service's model.
/// <para>
/// A record, so structural equality, <c>GetHashCode</c> and <c>ToString</c> come for free; the constructor
/// is private, so the only way in is <see cref="Of"/> and no instance can exist that breaks the rules below.
/// </para>
/// <para>
/// The amount is a <see cref="decimal"/> rounded to two places rather than an integer count of minor units.
/// Decimal is exact for the arithmetic done here and reads naturally in JSON, but it does assume every
/// currency has two decimals — which is false for the Japanese yen and for Tunisian millimes. Minor units
/// are the robust alternative and the README says so.
/// </para>
/// </remarks>
public sealed record Money
{
    private const int Decimals = 2;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>The amount, never negative, always rounded to two decimals.</summary>
    public decimal Amount { get; }

    /// <summary>The ISO-4217 alphabetic code, upper case, for example <c>EUR</c>.</summary>
    public string Currency { get; }

    /// <summary>Creates an amount, rejecting anything that is not a price.</summary>
    /// <exception cref="InvalidMoneyException">The amount is negative, or the currency code is malformed.</exception>
    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new InvalidMoneyException($"An amount of money cannot be negative, but was {amount}.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || !currency.All(char.IsAsciiLetter))
        {
            throw new InvalidMoneyException(
                $"'{currency}' is not an ISO-4217 currency code; three letters were expected, such as EUR.");
        }

        return new Money(Math.Round(amount, Decimals, MidpointRounding.ToEven), currency.ToUpperInvariant());
    }

    /// <summary>Nothing, in the given currency: where a sum starts.</summary>
    /// <exception cref="InvalidMoneyException">The currency code is malformed.</exception>
    public static Money Zero(string currency) => Of(0m, currency);

    /// <summary>Adds two amounts of the same currency.</summary>
    /// <exception cref="CurrencyMismatchException">The currencies differ.</exception>
    public Money Add(Money other)
    {
        EnsureSameCurrency(other);

        return Of(Amount + other.Amount, Currency);
    }

    /// <summary>Multiplies the amount, for example one seat price by a number of seats.</summary>
    /// <exception cref="InvalidMoneyException">The factor is negative.</exception>
    public Money Multiply(int factor)
    {
        if (factor < 0)
        {
            throw new InvalidMoneyException($"Money cannot be multiplied by a negative factor, but was {factor}.");
        }

        return Of(Amount * factor, Currency);
    }

    /// <summary>Renders the amount the way an invariant culture would, for logs and traces.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new CurrencyMismatchException(Currency, other.Currency);
        }
    }
}
