using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>Two amounts of money in different currencies were combined.</summary>
public sealed class CurrencyMismatchException : DomainException
{
    /// <summary>Creates the exception from the two currencies that did not match.</summary>
    public CurrencyMismatchException(string expected, string actual)
        : base(DomainErrorKind.Invalid, $"Amounts in {expected} and {actual} cannot be combined.")
    {
        AddExtension("expectedCurrency", expected);
        AddExtension("actualCurrency", actual);
    }
}
