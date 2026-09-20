using Microsoft.EntityFrameworkCore;

namespace QubicaCinema.BuildingBlocks.Persistence.Outbox;

/// <summary>Adds the outbox table to a service's model.</summary>
public static class OutboxModelExtensions
{
    /// <summary>
    /// Maps <see cref="OutboxMessage"/> into the model. Called explicitly from a <c>DbContext</c>'s
    /// <c>OnModelCreating</c>, for the same reasons as the idempotency table: the service's own assembly is
    /// the only one scanned, and a service that publishes nothing must not get the table by accident.
    /// </summary>
    public static ModelBuilder AddOutboxMessages(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        return modelBuilder;
    }
}
