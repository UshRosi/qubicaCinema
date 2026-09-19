using Microsoft.EntityFrameworkCore.ChangeTracking;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Infrastructure.Persistence.Configurations;

namespace QubicaCinema.Catalog.Infrastructure.Repositories;

/// <summary>Applies a caller's <c>If-Match</c> expectation to a tracked entity.</summary>
internal static class ConcurrencyGuard
{
    /// <summary>
    /// Refuses the edit unless the row still holds the version the caller read, and arms the database to
    /// refuse it as well if the row changes before the commit lands.
    /// </summary>
    /// <remarks>
    /// Both halves are needed, and each covers what the other cannot.
    /// <list type="bullet">
    /// <item><description>
    /// The comparison answers immediately and unconditionally. Setting the original value alone would not:
    /// when the new values happen to equal the stored ones, EF sees no change, sends no UPDATE, and the
    /// concurrency check silently never happens — so a caller working from a version three edits old would
    /// be told its write succeeded.
    /// </description></item>
    /// <item><description>
    /// The original value covers the race the comparison cannot: another request committing between this
    /// read and this commit. The UPDATE then matches no row and EF raises the conflict.
    /// </description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ConcurrentModificationException">The row has already moved on.</exception>
    internal static void RequireVersion<TEntity>(
        EntityEntry<TEntity> entry,
        ReadOnlyMemory<byte> expectedVersion,
        object id)
        where TEntity : class
    {
        PropertyEntry<TEntity, byte[]> version = entry.Property<byte[]>(RowVersion.Name);

        if (!expectedVersion.Span.SequenceEqual(version.CurrentValue))
        {
            throw new ConcurrentModificationException(typeof(TEntity).Name, id);
        }

        version.OriginalValue = expectedVersion.ToArray();
    }
}
