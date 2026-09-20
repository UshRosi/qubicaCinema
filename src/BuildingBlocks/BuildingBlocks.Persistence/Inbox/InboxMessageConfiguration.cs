using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QubicaCinema.BuildingBlocks.Persistence.Inbox;

/// <summary>Maps <see cref="InboxMessage"/> to the <c>InboxMessages</c> table.</summary>
internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(message => message.EventId);

        // The publisher's id, never generated here: it is the deduplication key.
        builder.Property(message => message.EventId).ValueGeneratedNever();

        builder.Property(message => message.EventName).HasMaxLength(200).IsRequired();
        builder.Property(message => message.ProcessedAt).IsRequired();
    }
}
