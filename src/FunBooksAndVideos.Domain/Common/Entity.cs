namespace FunBooksAndVideos.Domain.Common;

/// <summary>
/// Base class for entities: objects with identity. Two entities are equal when they are of the
/// same concrete type and share the same identifier.
/// </summary>
/// <typeparam name="TId">Strongly typed identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEquatable<TId>
{
    protected Entity(TId id)
    {
        Id = Guard.NotDefault(id, "entity.id.invalid", "Entity id");
    }

    public TId Id { get; }

    public bool Equals(Entity<TId>? other) =>
        other is not null && GetType() == other.GetType() && Id.Equals(other.Id);

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
