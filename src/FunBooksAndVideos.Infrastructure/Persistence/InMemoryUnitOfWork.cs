using FunBooksAndVideos.Domain.Persistence;

namespace FunBooksAndVideos.Infrastructure.Persistence;

/// <summary>
/// Scoped unit of work: repositories enlist operations, <see cref="CommitAsync"/> hands them to the store as one batch.
/// Staged operations are consumed by the commit attempt whether it succeeds, fails or is cancelled, so a retry starts clean.
/// Instances are not thread-safe; one instance serves one logical operation (request).
/// </summary>
internal sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly InMemoryDataStore _store;
    private readonly List<StoreOperation> _pending = [];

    public InMemoryUnitOfWork(InMemoryDataStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    internal int PendingOperationCount => _pending.Count;

    internal void Enlist(StoreOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _pending.Add(operation);
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        // Consume the batch before anything else, so a cancelled or failed attempt discards it as well.
        var batch = _pending.ToArray();
        _pending.Clear();

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (batch.Length == 0)
        {
            return Task.CompletedTask;
        }

        try
        {
            _store.Commit(batch);
            return Task.CompletedTask;
        }
        catch (Exception exception) when (exception is Domain.Persistence.PersistenceException or InvalidOperationException)
        {
            // Surface failures through the task, as callers of a Task-returning method expect.
            return Task.FromException(exception);
        }
    }
}
