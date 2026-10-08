namespace FunBooksAndVideos.Domain.Common;

/// <summary>
/// Consistency boundary that is loaded and persisted as a whole.
/// Carries an optimistic-concurrency <see cref="Version"/> maintained by the persistence layer.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : struct, IEquatable<TId>
{
    public const int InitialVersion = 0;

    protected AggregateRoot(TId id, int version)
        : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(version);
        Version = version;
    }

    /// <summary>
    /// Version of the persisted snapshot this instance was loaded from (<see cref="InitialVersion"/> for new aggregates).
    /// The persistence layer refuses to overwrite a snapshot that has moved on since the load.
    /// </summary>
    public int Version { get; }
}
