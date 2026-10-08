using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Mapping;

internal static class ShippingSlipRecordMapper
{
    public static ShippingSlipRecord ToRecord(ShippingSlip slip, int version) =>
        new(slip.Id.Value, slip.PurchaseOrderId.Value, slip.CustomerId.Value, slip.Address, slip.Items.ToArray(), slip.GeneratedAt, version);

    public static ShippingSlip ToDomain(ShippingSlipRecord record) =>
        ShippingSlip.Rehydrate(
            new ShippingSlipId(record.Id),
            new PurchaseOrderId(record.PurchaseOrderId),
            new CustomerId(record.CustomerId),
            record.Address,
            record.Items,
            record.GeneratedAt,
            record.Version);
}
