namespace QubicaCinema.BuildingBlocks.Api.Concurrency;

/// <summary>
/// Converts between a row's version and the entity tag a client sees.
/// </summary>
/// <remarks>
/// The tag is the store's own version token, base64-encoded and quoted as RFC 9110 requires. Strong, not
/// weak: it changes whenever the row changes, which is exactly what <c>If-Match</c> needs. The encoding is
/// opaque to clients by design — they echo back what they were given and never parse it.
/// </remarks>
public static class ETag
{
    /// <summary>Renders a version as a quoted entity tag, ready for the <c>ETag</c> header.</summary>
    public static string Format(ReadOnlyMemory<byte> version) => $"\"{Convert.ToBase64String(version.Span)}\"";

    /// <summary>Reads a version back out of an <c>If-Match</c> header value.</summary>
    /// <returns><see langword="true"/> when the header held a tag this service could have issued.</returns>
    public static bool TryParse(string? headerValue, out ReadOnlyMemory<byte> version)
    {
        version = default;

        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return false;
        }

        string trimmed = headerValue.Trim();

        // W/"…" is a weak tag: it means "semantically equivalent", which is not a safe basis for a write.
        if (trimmed.Length < 2 || trimmed[0] != '"' || trimmed[^1] != '"')
        {
            return false;
        }

        Span<byte> buffer = new byte[trimmed.Length];

        if (!Convert.TryFromBase64String(trimmed[1..^1], buffer, out int written))
        {
            return false;
        }

        version = buffer[..written].ToArray();

        return true;
    }

    /// <summary>
    /// Reads a version out of a header value that has already been checked by
    /// <see cref="IfMatchFilter"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The value is not a tag this service issued — which means the endpoint was mapped without the filter.
    /// </exception>
    public static ReadOnlyMemory<byte> Parse(string headerValue) =>
        TryParse(headerValue, out ReadOnlyMemory<byte> version)
            ? version
            : throw new InvalidOperationException(
                "The If-Match header was not validated before the endpoint ran; add RequiringIfMatch() to the route.");
}
