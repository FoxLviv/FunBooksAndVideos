using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Domain.Orders;

namespace FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;

/// <summary>Turns requested lines into priced domain lines by resolving products and membership plans from the catalog.</summary>
public interface IOrderLineFactory
{
    /// <exception cref="ValidationException">A referenced product or membership plan does not exist.</exception>
    Task<IReadOnlyList<OrderLine>> CreateAsync(IReadOnlyList<OrderLineRequest> requests, CancellationToken cancellationToken);
}
