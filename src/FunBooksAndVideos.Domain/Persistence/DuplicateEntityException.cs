namespace FunBooksAndVideos.Domain.Persistence;

/// <summary>An aggregate with the same identifier already exists.</summary>
public sealed class DuplicateEntityException : PersistenceException
{
    public const string ErrorCode = "persistence.duplicate_entity";

    public DuplicateEntityException(string entityName, object key)
        : base(ErrorCode, $"{entityName} {key} already exists.")
    {
        EntityName = entityName;
        Key = key;
    }

    public string EntityName { get; }

    public object Key { get; }
}
