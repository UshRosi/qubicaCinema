using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QubicaCinema.Catalog.Domain.Auditoriums;

namespace QubicaCinema.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Auditorium"/> to the <c>Auditoriums</c> table and its seats to <c>Seats</c>.</summary>
internal sealed class AuditoriumConfiguration : IEntityTypeConfiguration<Auditorium>
{
    public void Configure(EntityTypeBuilder<Auditorium> builder)
    {
        builder.ToTable("Auditoriums");
        builder.HasKey(auditorium => auditorium.Id);

        builder.Property(auditorium => auditorium.Name)
            .HasMaxLength(Auditorium.MaxNameLength)
            .IsRequired();

        builder.Property(auditorium => auditorium.RowCount).IsRequired();
        builder.Property(auditorium => auditorium.SeatsPerRow).IsRequired();

        // Computed from the grid; storing it would be a second source of truth that can disagree.
        builder.Ignore(auditorium => auditorium.Capacity);
        builder.Ignore(auditorium => auditorium.DomainEvents);

        // The rule "an auditorium name is unique" is stated by the model and enforced by the database,
        // because two concurrent requests both checking first would both pass. The violation is translated
        // back into the domain's own exception in CatalogDbContext.SaveChangesAsync.
        builder.HasIndex(auditorium => auditorium.Name).IsUnique();

        builder.HasMany(auditorium => auditorium.Seats)
            .WithOne()
            .HasForeignKey(seat => seat.AuditoriumId)
            // The seats are part of the aggregate: removing the room removes them.
            .OnDelete(DeleteBehavior.Cascade);

        // The property is read-only, so EF reads and writes the backing field directly instead of trying
        // to add to an IReadOnlyCollection — which is exactly the encapsulation the aggregate wants.
        builder.Metadata
            .FindNavigation(nameof(Auditorium.Seats))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
