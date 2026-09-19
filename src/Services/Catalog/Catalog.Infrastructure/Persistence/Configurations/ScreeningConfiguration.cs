using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QubicaCinema.Catalog.Domain.Screenings;

namespace QubicaCinema.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Screening"/> to the <c>Screenings</c> table.</summary>
internal sealed class ScreeningConfiguration : IEntityTypeConfiguration<Screening>
{
    public void Configure(EntityTypeBuilder<Screening> builder)
    {
        builder.ToTable("Screenings");
        builder.HasKey(screening => screening.Id);

        builder.Property(screening => screening.MovieId).IsRequired();
        builder.Property(screening => screening.AuditoriumId).IsRequired();

        // No foreign key to Movies or Auditoriums is declared on purpose beyond these two, which are in the
        // same database: see below. Within Catalog they are real relationships, so EF gets to enforce them.
        builder.HasOne<Domain.Movies.Movie>()
            .WithMany()
            .HasForeignKey(screening => screening.MovieId)
            // A film with screenings cannot be deleted out from under them.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Auditoriums.Auditorium>()
            .WithMany()
            .HasForeignKey(screening => screening.AuditoriumId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ComplexProperty(screening => screening.Slot, slot =>
        {
            slot.Property(value => value.StartsAt).HasColumnName("StartsAt").IsRequired();
            slot.Property(value => value.EndsAt).HasColumnName("EndsAt").IsRequired();
        });

        builder.ComplexProperty(screening => screening.Price, price =>
        {
            // 18,2 rather than the SQL Server default of 18,0, which would silently round every price to
            // the nearest euro — a default worth overriding in writing.
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

        builder.Property(screening => screening.CancellationReason).HasMaxLength(500);

        builder.Property<byte[]>(RowVersion.Name).IsRowVersion();

        builder.Ignore(screening => screening.DomainEvents);

        // The overlap check reads one auditorium's programme around one instant, and the default listing is
        // ordered by start time, so the index that serves both is (AuditoriumId, StartsAt). StartsAt lives
        // in a complex type, which EF Core cannot index, so that one is created as explicit SQL in the
        // initial migration. The foreign keys below already give AuditoriumId and MovieId an index each.
    }
}
