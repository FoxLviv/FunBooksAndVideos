namespace FunBooksAndVideos.Domain.Persistence;

/// <summary>The persisted aggregate changed since it was loaded (optimistic concurrency).</summary>
public sealed class ConcurrencyConflictException : PersistenceException
{
    public const string ErrorCode = "persistence.concurrency_conflict";

    public ConcurrencyConflictException(string entityName, object key)
        : base(ErrorCode, $"{entityName} {key} was modified concurrently. Reload it and retry the operation.")
    {
        EntityName = entityName;
        Key = key;
    }

    public string EntityName { get; }

    public object Key { get; }
}
