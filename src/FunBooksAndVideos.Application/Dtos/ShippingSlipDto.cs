namespace FunBooksAndVideos.Application.Dtos;

/// <summary>Packing instruction generated for the physical products of a purchase order.</summary>
/// <param name="Id">Shipping slip id.</param>
/// <param name="PurchaseOrderId">Order the slip belongs to.</param>
/// <param name="CustomerId">Recipient.</param>
/// <param name="Address">Where to ship.</param>
/// <param name="Items">What to pack.</param>
/// <param name="GeneratedAt">When the slip was generated (UTC).</param>
public sealed record ShippingSlipDto(
    long Id,
    long PurchaseOrderId,
    long CustomerId,
    ShippingAddressDto Address,
    IReadOnlyList<ShippingSlipItemDto> Items,
    DateTimeOffset GeneratedAt);

/// <summary>A product to pack.</summary>
/// <param name="ProductId">Product id.</param>
/// <param name="ProductName">Title.</param>
/// <param name="Quantity">Number of copies.</param>
public sealed record ShippingSlipItemDto(long ProductId, string ProductName, int Quantity);
