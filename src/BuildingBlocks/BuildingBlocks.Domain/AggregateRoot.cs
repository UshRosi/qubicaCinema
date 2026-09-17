namespace QubicaCinema.BuildingBlocks.Domain;

/// <summary>
/// The one entity of a cluster that the outside world is allowed to reach, and the boundary of a
/// transaction: everything inside it is saved together or not at all.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc />
    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <inheritdoc />
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// What this aggregate has decided since it was loaded, in order. Read-only outward: only the aggregate
    /// itself may record an event, so no caller can make the model claim something it never did.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Records an event as part of a state change. Called from inside a behaviour method.</summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Called once the events have been dispatched, or written to the outbox.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
