namespace QubicaCinema.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// The shadow property every versioned aggregate carries.
/// </summary>
/// <remarks>
/// Declared once so that the mapping, the repositories and the read-side projections cannot disagree about
/// the name — a typo here would compile and then fail at runtime with "no property 'Rowversion'".
/// </remarks>
internal static class RowVersion
{
    /// <summary>The property name, as EF Core knows it.</summary>
    internal const string Name = "RowVersion";
}
