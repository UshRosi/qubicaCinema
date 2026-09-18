namespace QubicaCinema.Catalog.Infrastructure.Extensions;

/// <summary>Calendar arithmetic on instants, of the kind a programme needs.</summary>
internal static class DateTimeOffsetExtensions
{
    /// <summary>Midnight UTC at the start of the day after this instant.</summary>
    /// <remarks>
    /// Explicitly UTC: taking <c>.Date</c> off a <see cref="DateTimeOffset"/> gives a <see cref="DateTime"/>
    /// of unspecified kind, which converting back reads with the machine's local offset — quietly moving the
    /// result by an hour or two depending on where it runs.
    /// </remarks>
    internal static DateTimeOffset StartOfNextUtcDay(this DateTimeOffset instant) =>
        new DateTimeOffset(instant.UtcDateTime.Date, TimeSpan.Zero).AddDays(1);

    /// <summary>
    /// Rounds up to the next multiple of <paramref name="granularity"/>, the way a printed programme turns
    /// 18:06 into 18:15. An instant already on a boundary is returned unchanged.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The granularity is zero or negative.</exception>
    internal static DateTimeOffset RoundUpTo(this DateTimeOffset instant, TimeSpan granularity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(granularity, TimeSpan.Zero);

        long step = granularity.Ticks;
        long rounded = (instant.Ticks + step - 1) / step * step;

        return new DateTimeOffset(rounded, instant.Offset);
    }
}
