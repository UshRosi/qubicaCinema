namespace QubicaCinema.BuildingBlocks.Api.Paging;

/// <summary>
/// The page a caller asked for, bound from the query string with <c>[AsParameters]</c>.
/// </summary>
/// <remarks>
/// The requested values are nullable and the usable ones are computed, so the clamp cannot be skipped:
/// there is no way to read a page size out of this type without it having passed through
/// <see cref="Size"/>. That matters because page size is a denial-of-service knob — <c>?pageSize=1000000</c>
/// is a cheap request that is expensive to answer — and clamping server-side is the only defence that a
/// client cannot opt out of.
/// <para>
/// Offset paging, not a cursor. It is what a reviewer expects from a catalogue API and it supports "jump to
/// page 7", at the known cost of drifting while rows are inserted and of deep pages getting slower. The
/// README names the trade-off.
/// </para>
/// </remarks>
/// <param name="Page">The one-based page number, or null for the first page.</param>
/// <param name="PageSize">How many items to return, or null for <see cref="DefaultSize"/>.</param>
public sealed record PageQuery(int? Page, int? PageSize)
{
    /// <summary>How many items a page holds when the caller does not say.</summary>
    public const int DefaultSize = 20;

    /// <summary>The most items one page can ever hold, whatever the caller asks for.</summary>
    public const int MaxSize = 100;

    /// <summary>The page to return, never below one.</summary>
    public int Number => Page is > 0 ? Page.Value : 1;

    /// <summary>The number of items to return, clamped into range.</summary>
    public int Size => PageSize is null ? DefaultSize : Math.Clamp(PageSize.Value, 1, MaxSize);

    /// <summary>How many items to skip to reach this page.</summary>
    public int Skip => (Number - 1) * Size;
}
