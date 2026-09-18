using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QubicaCinema.Catalog.Domain.Movies;

namespace QubicaCinema.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Movie"/> to the <c>Movies</c> table.</summary>
internal sealed class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.ToTable("Movies");
        builder.HasKey(movie => movie.Id);

        builder.Property(movie => movie.Title)
            .HasMaxLength(Movie.MaxTitleLength)
            .IsRequired();

        builder.Property(movie => movie.Description)
            .HasMaxLength(Movie.MaxDescriptionLength)
            .IsRequired();

        // Whole minutes rather than a SQL Server `time`: that type cannot hold 24 hours and would silently
        // cap a long film, and minutes are how a programme states a running time anyway.
        builder.Property(movie => movie.Duration)
            .HasColumnName("DurationMinutes")
            .HasConversion(
                duration => (int)duration.TotalMinutes,
                minutes => TimeSpan.FromMinutes(minutes));

        // Enums as their names, not their numbers: the column stays readable in a query window, and
        // reordering the enum cannot silently reinterpret existing rows.
        builder.Property(movie => movie.Genre)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(movie => movie.AgeRating)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        // The concurrency token is a shadow property: it is a fact about the row, not about the film, and
        // the domain has no business knowing that SQL Server stamps eight bytes on every update.
        builder.Property<byte[]>(RowVersion.Name).IsRowVersion();

        // Domain events are recorded in memory and dispatched by the outbox; they are never a column.
        builder.Ignore(movie => movie.DomainEvents);

        builder.HasIndex(movie => movie.Title);
    }
}
