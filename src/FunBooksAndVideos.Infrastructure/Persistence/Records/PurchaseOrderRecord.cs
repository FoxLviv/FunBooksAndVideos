using FunBooksAndVideos.Domain.Orders;

namespace FunBooksAndVideos.Infrastructure.Persistence.Records;

/// <summary>Immutable stored snapshot of a purchase order. Lines are immutable and can be shared safely.</summary>
internal sealed record PurchaseOrderRecord(
    long Id,
    long CustomerId,
    IReadOnlyList<OrderLine> Lines,
    PurchaseOrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    int Version) : IVersionedRecord;
