using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;

namespace FunBooksAndVideos.Application.Dtos;

/// <summary>Purchase order.</summary>
/// <param name="Id">Purchase order id.</param>
/// <param name="CustomerId">Customer the order belongs to.</param>
/// <param name="Total">Sum of all line prices.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Lines">Item lines.</param>
/// <param name="CreatedAt">When the order was submitted (UTC).</param>
/// <param name="ProcessedAt">When the business rules were applied (UTC); <see langword="null"/> while pending.</param>
public sealed record PurchaseOrderDto(
    long Id,
    long CustomerId,
    decimal Total,
    PurchaseOrderStatus Status,
    IReadOnlyList<OrderLineDto> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt);

/// <summary>Kind of purchase order line.</summary>
public enum OrderLineType
{
    /// <summary>A catalog product.</summary>
    Product = 1,

    /// <summary>A membership request.</summary>
    Membership = 2,
}

/// <summary>One item line. Product fields are set for product lines, membership fields for membership lines.</summary>
/// <param name="Type">Product or membership.</param>
/// <param name="Description">Human readable description, e.g. <c>Book "The Girl on the train"</c>.</param>
/// <param name="Price">Line price.</param>
/// <param name="ProductId">Product id (product lines only).</param>
/// <param name="ProductKind">Book or video (product lines only).</param>
/// <param name="RequiresShipping">Whether the product is physically delivered (product lines only).</param>
/// <param name="MembershipType">Membership type (membership lines only).</param>
public sealed record OrderLineDto(
    OrderLineType Type,
    string Description,
    decimal Price,
    long? ProductId,
    ProductKind? ProductKind,
    bool? RequiresShipping,
    MembershipType? MembershipType);
