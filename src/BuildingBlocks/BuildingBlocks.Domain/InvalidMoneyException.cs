namespace QubicaCinema.BuildingBlocks.Domain;

/// <summary>An amount of money was negative, or its currency code was not an ISO-4217 code.</summary>
public sealed class InvalidMoneyException(string message) : DomainException(DomainErrorKind.Invalid, message);
