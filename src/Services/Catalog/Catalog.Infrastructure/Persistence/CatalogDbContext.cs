using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Domain.Auditoriums;
using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.Movies;
using QubicaCinema.Catalog.Domain.Screenings;

namespace QubicaCinema.Catalog.Infrastructure.Persistence;

/// <summary>
/// The Catalog database.
/// </summary>
/// <remarks>
/// It is also the <see cref="IUnitOfWork"/>: the change tracker already is the transaction boundary of a
/// use case, so wrapping it in another type would add a layer that does nothing but forward. What matters
/// is the registration — resolving <see cref="IUnitOfWork"/> must return <em>this</em> instance, the one
/// the repositories used, or the commit would save an empty change tracker.
/// </remarks>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options), IUnitOfWork
{
    /// <summary>SQL Server's error numbers for a unique index and a unique constraint violation.</summary>
    private static readonly int[] UniqueViolationNumbers = [2601, 2627];

    /// <summary>The films the cinema can show.</summary>
    public DbSet<Movie> Movies => Set<Movie>();

    /// <summary>The rooms and their seats.</summary>
    public DbSet<Auditorium> Auditoriums => Set<Auditorium>();

    /// <summary>The programme.</summary>
    public DbSet<Screening> Screenings => Set<Screening>();

    /// <summary>
    /// Commits the use case, translating the store's failures into the domain's own vocabulary.
    /// </summary>
    /// <remarks>
    /// Translation happens here and nowhere else. A unique index and a row version are how SQL Server
    /// enforces two rules the model states; catching them in Application or in an endpoint would put
    /// <c>DbUpdateException</c> and SQL error numbers in layers that are not supposed to know what a
    /// database is — and every one of those places would need the same <c>try/catch</c>.
    /// </remarks>
    /// <exception cref="ConcurrentModificationException">Someone else changed a row first.</exception>
    /// <exception cref="AuditoriumNameAlreadyUsedException">Two auditoriums were given the same name.</exception>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            object? id = exception.Entries.Count > 0
                ? exception.Entries[0].Property(nameof(Entity<Guid>.Id)).CurrentValue
                : null;

            throw new ConcurrentModificationException(
                exception.Entries.Count > 0 ? exception.Entries[0].Metadata.DisplayName() : "The resource",
                id ?? "unknown",
                exception);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, out Auditorium? auditorium))
        {
            throw new AuditoriumNameAlreadyUsedException(auditorium.Name, exception);
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One IEntityTypeConfiguration per aggregate, discovered from this assembly: adding a mapping means
        // adding a file, and no central list can drift out of date.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    private static bool IsUniqueViolation(DbUpdateException exception, out Auditorium auditorium)
    {
        auditorium = null!;

        if (exception.InnerException is not SqlException sql || !UniqueViolationNumbers.Contains(sql.Number))
        {
            return false;
        }

        // Only the auditorium name is unique by business rule; anything else would be a bug, and a bug
        // should surface as itself rather than as a tidy 409.
        auditorium = exception.Entries
            .Select(entry => entry.Entity)
            .OfType<Auditorium>()
            .FirstOrDefault()!;

        return auditorium is not null;
    }
}
