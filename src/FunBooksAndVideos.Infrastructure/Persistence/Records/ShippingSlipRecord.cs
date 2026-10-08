using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Infrastructure.Persistence.Records;

/// <summary>Immutable stored snapshot of a shipping slip.</summary>
internal sealed record ShippingSlipRecord(
    long Id,
    long PurchaseOrderId,
    long CustomerId,
    ShippingAddress Address,
    IReadOnlyList<ShippingSlipItem> Items,
    DateTimeOffset GeneratedAt,
    int Version) : IVersionedRecord;
