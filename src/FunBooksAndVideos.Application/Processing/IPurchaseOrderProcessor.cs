using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;

namespace FunBooksAndVideos.Application.Processing;

/// <summary>Applies every applicable business rule to a pending purchase order and marks it processed.</summary>
public interface IPurchaseOrderProcessor
{
    /// <param name="order">A pending order.</param>
    /// <param name="customer">The account the order belongs to; rules may change it.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<PurchaseOrderProcessingResult> ProcessAsync(PurchaseOrder order, Customer customer, CancellationToken cancellationToken);
}
