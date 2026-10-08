using FunBooksAndVideos.Domain.Idempotency;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Mapping;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Repositories;

/// <summary>
/// Idempotency records keyed by the (case-sensitive) key. Inserts go through the unit of work, so a record is stored
/// atomically with the order it describes and a duplicate key is detected inside the commit.
/// </summary>
internal sealed class InMemoryIdempotencyRecordRepository : IIdempotencyRecordRepository
{
    private readonly InMemoryDataStore _store;
    private readonly InMemoryUnitOfWork _unitOfWork;

    public InMemoryIdempotencyRecordRepository(InMemoryDataStore store, InMemoryUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _store = store;
        _unitOfWork = unitOfWork;
    }

    public Task<IdempotencyRecord?> FindAsync(IdempotencyKey key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (key.Value is null)
        {
            return Task.FromResult<IdempotencyRecord?>(null);
        }

        var record = _store.Read(store => store.IdempotencyRecords.GetValueOrDefault(key.Value));
        return Task.FromResult(record is null ? null : IdempotencyRecordRecordMapper.ToDomain(record));
    }

    public void Add(IdempotencyRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var stored = IdempotencyRecordRecordMapper.ToRecord(record);
        _unitOfWork.Enlist(new InsertOperation<string, IdempotencyRecordRecord>(
            IdempotencyRecord.EntityName, store => store.IdempotencyRecords, stored.Key, stored));
    }
}
