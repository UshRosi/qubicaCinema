namespace QubicaCinema.Bookings.Infrastructure.Persistence.Configurations;

/// <summary>The shadow property a versioned aggregate carries, declared once so no two places spell it differently.</summary>
internal static class RowVersion
{
    /// <summary>The property name, as EF Core knows it.</summary>
    internal const string Name = "RowVersion";
}
