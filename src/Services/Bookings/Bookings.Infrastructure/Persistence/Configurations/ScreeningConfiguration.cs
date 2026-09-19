using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Infrastructure.Persistence.Configurations;

/// <summary>Maps Booking's copy of <see cref="Screening"/> to the <c>Screenings</c> table.</summary>
internal sealed class ScreeningConfiguration : IEntityTypeConfiguration<Screening>
{
    public void Configure(EntityTypeBuilder<Screening> builder)
    {
        builder.ToTable("Screenings");
        builder.HasKey(screening => screening.Id);

        // Catalog's id, never generated here: both services must mean the same screening by it.
        builder.Property(screening => screening.Id).ValueGeneratedNever();

        builder.Property(screening => screening.AuditoriumId).IsRequired();
        builder.Property(screening => screening.AuditoriumName).HasMaxLength(100).IsRequired();
        builder.Property(screening => screening.MovieTitle).HasMaxLength(200).IsRequired();
        builder.Property(screening => screening.StartsAt).IsRequired();

        builder.ComplexProperty(screening => screening.Price, price =>
        {
            price.Property(value => value.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(value => value.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Property(screening => screening.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Ignore(screening => screening.DomainEvents);
    }
}
