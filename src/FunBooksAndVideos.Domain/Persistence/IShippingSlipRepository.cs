using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Shipping;

namespace FunBooksAndVideos.Domain.Persistence;

public interface IShippingSlipRepository
{
    Task<ShippingSlip?> FindByPurchaseOrderAsync(PurchaseOrderId purchaseOrderId, CancellationToken cancellationToken);

    Task<ShippingSlipId> NextIdentityAsync(CancellationToken cancellationToken);

    /// <summary>Stages the insert; applied by <see cref="IUnitOfWork.CommitAsync"/>.</summary>
    void Add(ShippingSlip slip);
}
