using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>A screening was scheduled in the past.</summary>
public sealed class ScreeningInThePastException : DomainException
{
    /// <summary>Creates the exception from the requested start and the current time.</summary>
    public ScreeningInThePastException(DateTimeOffset startsAt, DateTimeOffset now)
        : base(DomainErrorKind.Invalid, $"A screening cannot start at {startsAt:u}, which is in the past.")
    {
        AddExtension("startsAt", startsAt);
        AddExtension("now", now);
    }
}
