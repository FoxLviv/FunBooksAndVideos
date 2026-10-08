using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.Infrastructure.Persistence.Mapping;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Repositories;

internal sealed class InMemoryShippingSlipRepository : IShippingSlipRepository
{
    private const string EntityName = "Shipping slip for purchase order";

    private readonly InMemoryDataStore _store;
    private readonly InMemoryUnitOfWork _unitOfWork;

    public InMemoryShippingSlipRepository(InMemoryDataStore store, InMemoryUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _store = store;
        _unitOfWork = unitOfWork;
    }

    public Task<ShippingSlip?> FindByPurchaseOrderAsync(PurchaseOrderId purchaseOrderId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = _store.Read(store => store.ShippingSlipsByPurchaseOrder.GetValueOrDefault(purchaseOrderId.Value));
        return Task.FromResult(record is null ? null : ShippingSlipRecordMapper.ToDomain(record));
    }

    public Task<ShippingSlipId> NextIdentityAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ShippingSlipId(_store.NextShippingSlipId()));
    }

    public void Add(ShippingSlip slip)
    {
        ArgumentNullException.ThrowIfNull(slip);

        var record = ShippingSlipRecordMapper.ToRecord(slip, slip.Version);
        _unitOfWork.Enlist(new InsertOperation<long, ShippingSlipRecord>(EntityName, store => store.ShippingSlipsByPurchaseOrder, record.PurchaseOrderId, record));
    }
}
