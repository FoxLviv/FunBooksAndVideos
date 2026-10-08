using FunBooksAndVideos.Domain.Orders;

namespace FunBooksAndVideos.Domain.Persistence;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> FindAsync(PurchaseOrderId id, CancellationToken cancellationToken);

    Task<PurchaseOrderId> NextIdentityAsync(CancellationToken cancellationToken);

    /// <summary>Stages the insert; applied by <see cref="IUnitOfWork.CommitAsync"/>.</summary>
    void Add(PurchaseOrder order);
}
