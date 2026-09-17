namespace QubicaCinema.BuildingBlocks.Domain;

/// <summary>
/// An object with its own identity and a lifecycle: two entities are the same when their ids are equal,
/// whatever their other values are.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    /// <summary>Creates the entity with its identity.</summary>
    protected Entity(TId id) => Id = id;

    /// <summary>Required by EF Core, which materialises entities without calling a real constructor.</summary>
    protected Entity() => Id = default!;

    /// <summary>The identity. Assigned once, when the entity is created.</summary>
    public TId Id { get; private set; }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other
        && other.GetType() == GetType()
        && EqualityComparer<TId>.Default.Equals(other.Id, Id);

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);
}
