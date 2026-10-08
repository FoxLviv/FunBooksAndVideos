using System.Collections.ObjectModel;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Shipping;

/// <summary>Instruction for the warehouse: what to pack for which order and where to send it.</summary>
public sealed class ShippingSlip : AggregateRoot<ShippingSlipId>
{
    private readonly ReadOnlyCollection<ShippingSlipItem> _items;

    private ShippingSlip(
        ShippingSlipId id,
        PurchaseOrderId purchaseOrderId,
        CustomerId customerId,
        ShippingAddress address,
        IEnumerable<ShippingSlipItem> items,
        DateTimeOffset generatedAt,
        int version)
        : base(id, version)
    {
        _items = items.ToList().AsReadOnly();
        if (_items.Count == 0)
        {
            throw new DomainValidationException("shipping_slip.items.empty", "A shipping slip must contain at least one item.");
        }

        if (_items.Any(item => item is null))
        {
            throw new DomainValidationException("shipping_slip.items.invalid", "Shipping slip items must not contain null entries.");
        }

        if (_items.Select(item => item.ProductId).Distinct().Count() != _items.Count)
        {
            throw new DomainValidationException("shipping_slip.items.duplicate", "Each product may appear only once on a shipping slip.");
        }

        PurchaseOrderId = Guard.NotDefault(purchaseOrderId, "shipping_slip.purchase_order_id.invalid", "Purchase order id");
        CustomerId = Guard.NotDefault(customerId, "shipping_slip.customer_id.invalid", "Customer id");
        Address = address ?? throw new DomainValidationException("shipping_slip.address.missing", "A shipping slip requires a shipping address.");
        GeneratedAt = generatedAt;
    }

    public PurchaseOrderId PurchaseOrderId { get; }

    public CustomerId CustomerId { get; }

    public ShippingAddress Address { get; }

    public IReadOnlyList<ShippingSlipItem> Items => _items;

    public DateTimeOffset GeneratedAt { get; }

    public static ShippingSlip Create(
        ShippingSlipId id,
        PurchaseOrderId purchaseOrderId,
        CustomerId customerId,
        ShippingAddress address,
        IEnumerable<ShippingSlipItem> items,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new ShippingSlip(id, purchaseOrderId, customerId, address, items, generatedAt, InitialVersion);
    }

    /// <summary>Rebuilds a slip from a persisted snapshot. Only the persistence layer should call this.</summary>
    public static ShippingSlip Rehydrate(
        ShippingSlipId id,
        PurchaseOrderId purchaseOrderId,
        CustomerId customerId,
        ShippingAddress address,
        IEnumerable<ShippingSlipItem> items,
        DateTimeOffset generatedAt,
        int version)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new ShippingSlip(id, purchaseOrderId, customerId, address, items, generatedAt, version);
    }
}
