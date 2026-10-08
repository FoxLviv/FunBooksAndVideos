using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Shipping;

namespace FunBooksAndVideos.Application.Mapping;

public static class ShippingSlipMapper
{
    public static ShippingSlipDto ToDto(ShippingSlip slip)
    {
        ArgumentNullException.ThrowIfNull(slip);

        return new ShippingSlipDto(
            slip.Id.Value,
            slip.PurchaseOrderId.Value,
            slip.CustomerId.Value,
            ShippingAddressMapper.ToDto(slip.Address),
            slip.Items.Select(item => new ShippingSlipItemDto(item.ProductId.Value, item.ProductName, item.Quantity)).ToList(),
            slip.GeneratedAt);
    }
}
