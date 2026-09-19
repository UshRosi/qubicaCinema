using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QubicaCinema.Catalog.Domain.Auditoriums;

namespace QubicaCinema.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Seat"/> to the <c>Seats</c> table.</summary>
/// <remarks>
/// A table of its own, not an owned collection on the auditorium: a seat has an identity that Booking
/// refers to for the life of a booking, so it needs a stable primary key rather than a position in a list.
/// </remarks>
internal sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seats");
        builder.HasKey(seat => seat.Id);

        // A complex type, not an owned entity: the position has no identity, and mapping it this way keeps
        // its columns on the seat's own entity type — which is what lets the unique index below span the
        // auditorium and the position together. An owned type would be a second entity type sharing the
        // table, and an index cannot reach across two of those.
        builder.ComplexProperty(seat => seat.Position, position =>
        {
            position.Property(value => value.Row)
                .HasColumnName("Row")
                .HasMaxLength(1)
                .IsRequired();

            position.Property(value => value.Number)
                .HasColumnName("Number")
                .IsRequired();
        });

        builder.HasIndex(seat => seat.AuditoriumId);

        // The rule "no two seats in one room claim the same place" needs an index over AuditoriumId
        // together with the two position columns, and EF Core cannot declare an index that reaches into a
        // complex type. It is therefore created as explicit SQL in the initial migration, named there, and
        // never dropped — EF compares the model against its own snapshot, so an index it does not know
        // about is left alone. The aggregate enforces the same rule when it builds the grid; the index is
        // what holds if anything ever writes seats without going through it.
    }
}
