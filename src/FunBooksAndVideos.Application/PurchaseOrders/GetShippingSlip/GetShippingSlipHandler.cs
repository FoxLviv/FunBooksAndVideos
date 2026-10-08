using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Application.Mapping;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;

namespace FunBooksAndVideos.Application.PurchaseOrders.GetShippingSlip;

public sealed class GetShippingSlipHandler : IQueryHandler<GetShippingSlipQuery, ShippingSlipDto>
{
    private readonly IPurchaseOrderRepository _purchaseOrders;
    private readonly IShippingSlipRepository _shippingSlips;

    public GetShippingSlipHandler(IPurchaseOrderRepository purchaseOrders, IShippingSlipRepository shippingSlips)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrders);
        ArgumentNullException.ThrowIfNull(shippingSlips);

        _purchaseOrders = purchaseOrders;
        _shippingSlips = shippingSlips;
    }

    public async Task<ShippingSlipDto> HandleAsync(GetShippingSlipQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var orderId = new PurchaseOrderId(query.PurchaseOrderId);

        var slip = await _shippingSlips.FindByPurchaseOrderAsync(orderId, cancellationToken);
        if (slip is not null)
        {
            return ShippingSlipMapper.ToDto(slip);
        }

        // Only the miss needs the order: to tell "no such order" from "order without physical products".
        _ = await _purchaseOrders.FindAsync(orderId, cancellationToken)
            ?? throw new NotFoundException("purchase_order.not_found", $"Purchase order {orderId} was not found.");

        throw new NotFoundException("shipping_slip.not_found", $"Purchase order {orderId} has no shipping slip; it contains no physical products.");
    }
}
