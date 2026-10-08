namespace FunBooksAndVideos.Domain.Persistence;

/// <summary>Base type for failures reported by the persistence contracts.</summary>
public abstract class PersistenceException : Exception
{
    protected PersistenceException(string code, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    /// <summary>Stable error code, e.g. <c>persistence.concurrency_conflict</c>.</summary>
    public string Code { get; }
}
