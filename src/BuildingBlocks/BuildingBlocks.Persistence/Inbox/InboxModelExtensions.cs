using Microsoft.EntityFrameworkCore;

namespace QubicaCinema.BuildingBlocks.Persistence.Inbox;

/// <summary>Adds the inbox table to a service's model.</summary>
public static class InboxModelExtensions
{
    /// <summary>
    /// Maps <see cref="InboxMessage"/> into the model. Called explicitly from a <c>DbContext</c>'s
    /// <c>OnModelCreating</c>, so that only a service that consumes events gets the table.
    /// </summary>
    public static ModelBuilder AddInboxMessages(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());

        return modelBuilder;
    }
}
