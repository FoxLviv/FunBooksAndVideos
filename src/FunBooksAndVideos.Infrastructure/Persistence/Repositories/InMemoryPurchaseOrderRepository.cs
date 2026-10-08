using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Mapping;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Repositories;

internal sealed class InMemoryPurchaseOrderRepository : IPurchaseOrderRepository
{
    private const string EntityName = "Purchase order";

    private readonly InMemoryDataStore _store;
    private readonly InMemoryUnitOfWork _unitOfWork;

    public InMemoryPurchaseOrderRepository(InMemoryDataStore store, InMemoryUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _store = store;
        _unitOfWork = unitOfWork;
    }

    public Task<PurchaseOrder?> FindAsync(PurchaseOrderId id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = _store.Read(store => store.PurchaseOrders.GetValueOrDefault(id.Value));
        return Task.FromResult(record is null ? null : PurchaseOrderRecordMapper.ToDomain(record));
    }

    public Task<PurchaseOrderId> NextIdentityAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PurchaseOrderId(_store.NextPurchaseOrderId()));
    }

    public void Add(PurchaseOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var record = PurchaseOrderRecordMapper.ToRecord(order, order.Version);
        _unitOfWork.Enlist(new InsertOperation<long, PurchaseOrderRecord>(EntityName, store => store.PurchaseOrders, record.Id, record));
    }
}
