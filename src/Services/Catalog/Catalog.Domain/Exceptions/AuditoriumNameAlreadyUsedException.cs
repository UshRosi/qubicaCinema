using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.Catalog.Domain.Exceptions;

/// <summary>Another auditorium already carries this name.</summary>
public sealed class AuditoriumNameAlreadyUsedException : DomainException
{
    /// <summary>Creates the exception from the name that was taken.</summary>
    public AuditoriumNameAlreadyUsedException(string name)
        : base(DomainErrorKind.Conflict, $"An auditorium named '{name}' already exists.") =>
        AddExtension("name", name);

    /// <summary>Creates the exception from the unique-index violation that revealed the clash.</summary>
    public AuditoriumNameAlreadyUsedException(string name, Exception innerException)
        : base(DomainErrorKind.Conflict, $"An auditorium named '{name}' already exists.", innerException) =>
        AddExtension("name", name);
}
