namespace QubicaCinema.BuildingBlocks.Application.Results;

/// <summary>
/// A read model together with the version of the row it was read from.
/// </summary>
/// <remarks>
/// The version is what the API turns into an <c>ETag</c>, and what a later <c>If-Match</c> is checked
/// against. It is kept beside the view rather than inside it because it is not part of the resource: a
/// client echoes it back in a header and never looks at it, and putting it in the JSON body would invite
/// exactly the parsing that entity tags are meant to prevent.
/// </remarks>
/// <typeparam name="T">The read model.</typeparam>
/// <param name="Value">What was read.</param>
/// <param name="Version">
/// The row version it was read at. A <see cref="ReadOnlyMemory{T}"/> rather than a <c>byte[]</c>: handing
/// out the array would hand out something a caller could write to.
/// </param>
public sealed record Versioned<T>(T Value, ReadOnlyMemory<byte> Version);
