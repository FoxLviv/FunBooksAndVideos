using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence;

/// <summary>
/// A staged change (GoF Command). The unit of work collects operations; the store validates all of them
/// and only then applies all of them, which makes a commit atomic.
/// </summary>
internal abstract class StoreOperation
{
    /// <summary>Checks the operation against the current store state. Must not change anything.</summary>
    /// <param name="store">The store, locked by the caller.</param>
    /// <param name="touchedKeys">Keys already claimed by earlier operations of the same batch.</param>
    internal abstract void Validate(InMemoryDataStore store, ISet<(string EntityName, object Key)> touchedKeys);

    /// <summary>Applies the operation. Only called after every operation of the batch validated.</summary>
    internal abstract void Apply(InMemoryDataStore store);
}

/// <summary>Inserts a new record; fails when the key exists.</summary>
internal sealed class InsertOperation<TKey, TRecord> : StoreOperation
    where TKey : notnull
{
    private readonly string _entityName;
    private readonly Func<InMemoryDataStore, Dictionary<TKey, TRecord>> _table;
    private readonly TKey _key;
    private readonly TRecord _record;

    public InsertOperation(string entityName, Func<InMemoryDataStore, Dictionary<TKey, TRecord>> table, TKey key, TRecord record)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(record);

        _entityName = entityName;
        _table = table;
        _key = key;
        _record = record;
    }

    internal override void Validate(InMemoryDataStore store, ISet<(string EntityName, object Key)> touchedKeys)
    {
        if (_table(store).ContainsKey(_key) || !touchedKeys.Add((_entityName, _key)))
        {
            throw new DuplicateEntityException(_entityName, _key);
        }
    }

    internal override void Apply(InMemoryDataStore store) => _table(store)[_key] = _record;
}

/// <summary>Replaces an existing record; fails when it is missing or its version moved on since it was loaded.</summary>
internal sealed class UpdateOperation<TKey, TRecord> : StoreOperation
    where TKey : notnull
    where TRecord : IVersionedRecord
{
    private readonly string _entityName;
    private readonly Func<InMemoryDataStore, Dictionary<TKey, TRecord>> _table;
    private readonly TKey _key;
    private readonly int _expectedVersion;
    private readonly Func<int, TRecord> _createRecord;

    /// <param name="entityName">Human readable entity name for error messages.</param>
    /// <param name="table">Selects the table.</param>
    /// <param name="key">Record key.</param>
    /// <param name="expectedVersion">Version the aggregate was loaded with.</param>
    /// <param name="createRecord">Builds the new snapshot for the given (incremented) version.</param>
    public UpdateOperation(
        string entityName,
        Func<InMemoryDataStore, Dictionary<TKey, TRecord>> table,
        TKey key,
        int expectedVersion,
        Func<int, TRecord> createRecord)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(createRecord);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedVersion);

        _entityName = entityName;
        _table = table;
        _key = key;
        _expectedVersion = expectedVersion;
        _createRecord = createRecord;
    }

    internal override void Validate(InMemoryDataStore store, ISet<(string EntityName, object Key)> touchedKeys)
    {
        if (!touchedKeys.Add((_entityName, _key)))
        {
            throw new InvalidOperationException($"{_entityName} {_key} is staged more than once in the same unit of work.");
        }

        if (!_table(store).TryGetValue(_key, out var current) || current.Version != _expectedVersion)
        {
            throw new ConcurrencyConflictException(_entityName, _key);
        }
    }

    internal override void Apply(InMemoryDataStore store) => _table(store)[_key] = _createRecord(_expectedVersion + 1);
}
