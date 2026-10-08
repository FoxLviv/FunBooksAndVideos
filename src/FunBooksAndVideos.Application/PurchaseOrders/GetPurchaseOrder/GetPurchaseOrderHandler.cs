using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Application.Mapping;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;

namespace FunBooksAndVideos.Application.PurchaseOrders.GetPurchaseOrder;

public sealed class GetPurchaseOrderHandler : IQueryHandler<GetPurchaseOrderQuery, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _purchaseOrders;

    public GetPurchaseOrderHandler(IPurchaseOrderRepository purchaseOrders)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrders);
        _purchaseOrders = purchaseOrders;
    }

    public async Task<PurchaseOrderDto> HandleAsync(GetPurchaseOrderQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var order = await _purchaseOrders.FindAsync(new PurchaseOrderId(query.PurchaseOrderId), cancellationToken)
            ?? throw new NotFoundException("purchase_order.not_found", $"Purchase order {query.PurchaseOrderId} was not found.");

        return PurchaseOrderMapper.ToDto(order);
    }
}
