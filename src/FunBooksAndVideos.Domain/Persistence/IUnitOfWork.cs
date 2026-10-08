namespace FunBooksAndVideos.Domain.Persistence;

/// <summary>
/// Unit of Work: repositories stage changes, <see cref="CommitAsync"/> applies all of them atomically.
/// When the commit fails or is cancelled nothing is applied and the staged changes are discarded.
/// </summary>
public interface IUnitOfWork
{
    /// <exception cref="ConcurrencyConflictException">An aggregate was modified by someone else since it was loaded.</exception>
    /// <exception cref="DuplicateEntityException">An aggregate with the same identifier already exists.</exception>
    Task CommitAsync(CancellationToken cancellationToken);
}
