namespace QubicaCinema.Bookings.Infrastructure.Extensions;

/// <summary>Calendar arithmetic on instants.</summary>
internal static class DateTimeOffsetExtensions
{
    /// <summary>Midnight UTC at the start of the day after this instant.</summary>
    /// <remarks>
    /// Explicitly UTC: <c>.Date</c> on a <see cref="DateTimeOffset"/> gives a <see cref="DateTime"/> of
    /// unspecified kind, which converting back reads with the machine's local offset.
    /// </remarks>
    internal static DateTimeOffset StartOfNextUtcDay(this DateTimeOffset instant) =>
        new DateTimeOffset(instant.UtcDateTime.Date, TimeSpan.Zero).AddDays(1);
}
