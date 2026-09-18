using Microsoft.EntityFrameworkCore;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Domain.Auditoriums;
using QubicaCinema.Catalog.Infrastructure.Persistence;

namespace QubicaCinema.Catalog.Infrastructure.Repositories;

/// <inheritdoc cref="IAuditoriumRepository" />
internal sealed class AuditoriumRepository(CatalogDbContext context) : IAuditoriumRepository
{
    /// <inheritdoc />
    public Task<Auditorium?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Auditoriums
            // The seats are part of the aggregate, so loading one without them would produce a room that
            // believes it has no seats — and the model would then let it be "changed" on that basis.
            .Include(auditorium => auditorium.Seats)
            .FirstOrDefaultAsync(auditorium => auditorium.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Auditoriums.AnyAsync(auditorium => auditorium.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> NameIsTakenAsync(string name, CancellationToken cancellationToken) =>
        context.Auditoriums.AnyAsync(auditorium => auditorium.Name == name, cancellationToken);

    /// <inheritdoc />
    public void Add(Auditorium auditorium) => context.Auditoriums.Add(auditorium);
}
