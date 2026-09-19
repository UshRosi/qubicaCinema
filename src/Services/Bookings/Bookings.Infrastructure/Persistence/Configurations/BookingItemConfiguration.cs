using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Screenings;

namespace QubicaCinema.Bookings.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="BookingItem"/> to the <c>BookingItems</c> table.</summary>
internal sealed class BookingItemConfiguration : IEntityTypeConfiguration<BookingItem>
{
    /// <summary>
    /// The index that prevents double booking. Named, because the DbContext recognises its violation by
    /// name and turns it into <c>SeatAlreadyBookedException</c>.
    /// </summary>
    internal const string DoubleBookingIndexName = "IX_BookingItems_Screening_Seat";

    /// <summary>
    /// The foreign key to the booking, a shadow property: an item lives inside its booking and has no need
    /// to point back at it.
    /// </summary>
    internal const string BookingIdProperty = "BookingId";

    public void Configure(EntityTypeBuilder<BookingItem> builder)
    {
        builder.ToTable("BookingItems");
        builder.HasKey(item => item.Id);

        builder.Property<Guid>(BookingIdProperty);

        builder.ComplexProperty(item => item.Price, price =>
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

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Ignore(item => item.IsActive);

        // The screenings and seats are the local read model, in the same database, so these are real
        // foreign keys. Restrict: a screening or a seat that a booking refers to is never removed.
        builder.HasOne<Screening>()
            .WithMany()
            .HasForeignKey(item => item.ScreeningId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Seat>()
            .WithMany()
            .HasForeignKey(item => item.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        // THE rule of this service, enforced where it cannot be raced: one active item per seat and
        // screening. Filtered on Active, so a cancelled item keeps its row and its seat becomes bookable
        // again. Checking the seat map first gives a friendly answer in the usual case; this index is what
        // decides when two requests saw the same seat free at the same moment. It also serves the query
        // "which seats are taken at this screening?", which is exactly its key and its filter.
        builder.HasIndex(item => new { item.ScreeningId, item.SeatId })
            .IsUnique()
            .HasFilter("[Status] = 'Active'")
            .HasDatabaseName(DoubleBookingIndexName);
    }
}
