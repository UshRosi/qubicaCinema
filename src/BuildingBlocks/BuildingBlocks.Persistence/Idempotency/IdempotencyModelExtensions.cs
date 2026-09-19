using Microsoft.EntityFrameworkCore;

namespace QubicaCinema.BuildingBlocks.Persistence.Idempotency;

/// <summary>Adds the idempotency table to a service's model.</summary>
public static class IdempotencyModelExtensions
{
    /// <summary>
    /// Maps <see cref="IdempotencyRecord"/> into the model.
    /// </summary>
    /// <remarks>
    /// Called explicitly from a <c>DbContext</c>'s <c>OnModelCreating</c>, because
    /// <c>ApplyConfigurationsFromAssembly</c> only finds configurations in the service's own assembly —
    /// and because a service that stores nothing idempotently should not get the table by accident.
    /// </remarks>
    public static ModelBuilder AddIdempotencyRecords(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new IdempotencyRecordConfiguration());

        return modelBuilder;
    }
}
