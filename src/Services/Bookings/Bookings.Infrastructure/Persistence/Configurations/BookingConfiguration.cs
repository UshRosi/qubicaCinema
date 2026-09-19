using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QubicaCinema.Bookings.Domain.Bookings;

namespace QubicaCinema.Bookings.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Booking"/> to the <c>Bookings</c> table and its items to <c>BookingItems</c>.</summary>
internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.UserId).IsRequired();

        builder.Property(booking => booking.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(booking => booking.CreatedAt).IsRequired();

        // Guards concurrent edits of the same booking — two cancellations racing each other. It has
        // nothing to do with two customers wanting the same seat: that is the filtered unique index on
        // BookingItems, a different mechanism for a different race. A shadow property, as in Catalog.
        builder.Property<byte[]>(RowVersion.Name).IsRowVersion();

        builder.HasMany(booking => booking.Items)
            .WithOne()
            .HasForeignKey(BookingItemConfiguration.BookingIdProperty)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Booking.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Computed from the items; storing any of them would be a second source of truth.
        builder.Ignore(booking => booking.Total);
        builder.Ignore(booking => booking.ActiveItems);
        builder.Ignore(booking => booking.ScreeningIds);
        builder.Ignore(booking => booking.DomainEvents);

        // "My bookings, newest first" is the list every customer sees.
        builder.HasIndex(booking => new { booking.UserId, booking.CreatedAt });
    }
}
