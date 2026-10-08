using FunBooksAndVideos.Domain.Idempotency;

namespace FunBooksAndVideos.Domain.Persistence;

/// <summary>Stores the outcome of requests submitted with an idempotency key.</summary>
public interface IIdempotencyRecordRepository
{
    /// <summary>Returns the record stored for <paramref name="key"/>, or <see langword="null"/> when the key was never used.</summary>
    Task<IdempotencyRecord?> FindAsync(IdempotencyKey key, CancellationToken cancellationToken);

    /// <summary>
    /// Stages the insert; applied by <see cref="IUnitOfWork.CommitAsync"/>. A key that already exists makes the commit fail with
    /// <see cref="DuplicateEntityException"/> (entity name <see cref="IdempotencyRecord.EntityName"/>).
    /// </summary>
    void Add(IdempotencyRecord record);
}
