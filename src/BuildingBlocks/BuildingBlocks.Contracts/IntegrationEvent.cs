namespace QubicaCinema.BuildingBlocks.Contracts;

/// <summary>
/// Something that happened in one service and that another service may want to react to.
/// </summary>
/// <remarks>
/// An integration event is a wire contract, and there is no synchronised deployment: a message can be
/// written by one version of a service and read by another, or replayed days later. Every event therefore
/// follows the same extend-only rules.
/// <list type="number">
///   <item>The wire name is a stable logical string such as <c>catalog.screening-scheduled.v1</c>, never a
///   CLR or assembly-qualified type name. Renaming a class must not make a stored message unreadable.</item>
///   <item>A published field is never removed, renamed or retyped. A rename fails silently, because
///   System.Text.Json leaves the property at its default instead of throwing.</item>
///   <item>A new field is optional and has a meaningful default, so that old publishers may omit it.</item>
///   <item>A change of meaning is a new type with a new name (<c>…v2</c>); the v1 record stays exactly as it
///   shipped. Deploy the reader first, then switch the writer, then retire v1.</item>
///   <item>Readers are tolerant: unknown properties are ignored.</item>
///   <item>Only primitives cross the wire, never a domain entity.</item>
/// </list>
/// </remarks>
public abstract record IntegrationEvent
{
    /// <summary>
    /// Identifies this occurrence. It is the consumer's deduplication key, and being a version 7 GUID it
    /// also sorts by creation time, which keeps an index over it append-only.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    /// <summary>
    /// When it happened, as decided by the model that raised it. Required rather than defaulted to the
    /// clock: the time comes from a <see cref="TimeProvider"/> and travels with the event unchanged.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>The stable wire name of this event. It doubles as the routing key.</summary>
    /// <remarks>Left out of the payload by the event serializer: the name travels beside it, not inside it.</remarks>
    public abstract string EventName { get; }
}
