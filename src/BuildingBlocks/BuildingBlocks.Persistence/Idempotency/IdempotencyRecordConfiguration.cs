using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QubicaCinema.BuildingBlocks.Persistence.Idempotency;

/// <summary>Maps <see cref="IdempotencyRecord"/> to the <c>IdempotencyRecords</c> table.</summary>
internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    /// <summary>The primary key, named so its violation can be recognised as "this key is taken".</summary>
    internal const string PrimaryKeyName = "PK_IdempotencyRecords";

    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        // The user is part of the key: two customers may pick the same key without colliding, and nobody
        // can find out whether a key exists for somebody else.
        builder.HasKey(record => new { record.UserId, record.Key }).HasName(PrimaryKeyName);

        builder.Property(record => record.Key).HasMaxLength(100).IsUnicode(false);
        builder.Property(record => record.RequestHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();

        // Null only inside the transaction that claims the key; never null once it has committed.
        builder.Property(record => record.StatusCode);
        builder.Property(record => record.ResponseBody);

        builder.Property(record => record.CreatedAt).IsRequired();
    }
}
