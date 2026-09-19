namespace QubicaCinema.BuildingBlocks.Domain;

/// <summary>
/// Two amounts in different currencies were combined — for a booking, screenings priced in different
/// currencies were put in one basket, which has no single total to charge.
/// </summary>
/// <remarks>
/// A conflict rather than an invalid request: nothing in the payload says what a screening costs, so a
/// client cannot tell from its own body that the two do not go together.
/// </remarks>
public sealed class CurrencyMismatchException : DomainException
{
    /// <summary>Creates the exception from the two currencies that did not match.</summary>
    public CurrencyMismatchException(string expected, string actual)
        : base(DomainErrorKind.Conflict, $"Amounts in {expected} and {actual} cannot be combined.")
    {
        AddExtension("expectedCurrency", expected);
        AddExtension("actualCurrency", actual);
    }
}
