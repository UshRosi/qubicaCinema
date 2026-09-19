using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;

namespace QubicaCinema.BuildingBlocks.UnitTests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Should_normalise_the_currency_code_to_upper_case()
    {
        var price = Money.Of(9.5m, "eur");

        price.Currency.ShouldBe("EUR");
    }

    [Theory]
    [InlineData(9.994, 9.99)]
    [InlineData(9.995, 10.00)]
    [InlineData(9.996, 10.00)]
    public void Should_round_the_amount_to_two_decimals(decimal given, decimal expected)
    {
        Money.Of(given, "EUR").Amount.ShouldBe(expected);
    }

    [Fact]
    public void Should_reject_a_negative_amount()
    {
        Should.Throw<InvalidMoneyException>(() => Money.Of(-0.01m, "EUR"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("3UR")]
    public void Should_reject_a_currency_that_is_not_an_iso_code(string currency)
    {
        Should.Throw<InvalidMoneyException>(() => Money.Of(9.5m, currency));
    }

    [Fact]
    public void Should_be_equal_to_another_amount_with_the_same_value()
    {
        Money.Of(9.50m, "EUR").ShouldBe(Money.Of(9.5m, "eur"));
    }

    [Fact]
    public void Should_add_two_amounts_of_the_same_currency()
    {
        var total = Money.Of(9.5m, "EUR").Add(Money.Of(0.75m, "EUR"));

        total.Amount.ShouldBe(10.25m);
    }

    [Fact]
    public void Should_refuse_to_add_two_different_currencies()
    {
        CurrencyMismatchException exception =
            Should.Throw<CurrencyMismatchException>(() => Money.Of(9.5m, "EUR").Add(Money.Of(1m, "USD")));

        exception.Extensions["expectedCurrency"].ShouldBe("EUR");
        exception.Extensions["actualCurrency"].ShouldBe("USD");
    }

    [Fact]
    public void Should_multiply_a_seat_price_by_a_number_of_seats()
    {
        Money.Of(9.5m, "EUR").Multiply(3).Amount.ShouldBe(28.5m);
    }

    [Fact]
    public void Should_reject_a_negative_factor()
    {
        Should.Throw<InvalidMoneyException>(() => Money.Of(9.5m, "EUR").Multiply(-1));
    }
}
