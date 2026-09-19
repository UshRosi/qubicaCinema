using QubicaCinema.Catalog.Domain.Auditoriums;

namespace QubicaCinema.Catalog.Application.Abstractions.Repositories;

/// <summary>Loads and stores <see cref="Auditorium"/> aggregates.</summary>
public interface IAuditoriumRepository
{
    /// <summary>Loads an auditorium with its seats, or null when there is none with that id.</summary>
    Task<Auditorium?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Whether an auditorium with this id exists, without loading its seats.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Whether an auditorium already carries this name.</summary>
    Task<bool> NameIsTakenAsync(string name, CancellationToken cancellationToken);

    /// <summary>Stages a new auditorium and every seat in it.</summary>
    void Add(Auditorium auditorium);
}
