using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Infrastructure.Persistence.Configurations;

/// <summary>Maps Booking's copy of <see cref="Seat"/> to the <c>Seats</c> table.</summary>
internal sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seats");
        builder.HasKey(seat => seat.Id);

        // Catalog's id, never generated here.
        builder.Property(seat => seat.Id).ValueGeneratedNever();

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

        // Every seat map reads one auditorium's seats.
        builder.HasIndex(seat => seat.AuditoriumId);
    }
}
