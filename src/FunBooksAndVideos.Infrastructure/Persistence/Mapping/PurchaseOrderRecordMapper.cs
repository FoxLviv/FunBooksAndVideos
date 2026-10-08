using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Mapping;

internal static class PurchaseOrderRecordMapper
{
    public static PurchaseOrderRecord ToRecord(PurchaseOrder order, int version) =>
        new(order.Id.Value, order.CustomerId.Value, order.Lines.ToArray(), order.Status, order.CreatedAt, order.ProcessedAt, version);

    public static PurchaseOrder ToDomain(PurchaseOrderRecord record) =>
        PurchaseOrder.Rehydrate(
            new PurchaseOrderId(record.Id),
            new CustomerId(record.CustomerId),
            record.Lines,
            record.Status,
            record.CreatedAt,
            record.ProcessedAt,
            record.Version);
}
