using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;

namespace FunBooksAndVideos.Domain.Shipping;

/// <summary>
/// Domain service that knows how to turn the shippable part of a purchase order into a shipping slip.
/// Keeps the packing logic (grouping repeated products into quantities, requiring an address) in the domain.
/// </summary>
public static class ShippingSlipFactory
{
    public static ShippingSlip CreateFor(ShippingSlipId id, PurchaseOrder order, Customer customer, DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(customer);

        if (order.CustomerId != customer.Id)
        {
            throw new DomainValidationException(
                "shipping_slip.customer.mismatch",
                $"Purchase order {order.Id} belongs to customer {order.CustomerId}, not to customer {customer.Id}.");
        }

        if (!order.RequiresShipping)
        {
            throw new BusinessRuleViolationException(
                "shipping_slip.nothing_to_ship",
                $"Purchase order {order.Id} contains no physical products.");
        }

        if (customer.ShippingAddress is null)
        {
            throw new BusinessRuleViolationException(
                "customer.shipping_address.missing",
                $"Customer {customer.Id} has no shipping address, but purchase order {order.Id} contains physical products.");
        }

        var items = order.ShippableLines
            .GroupBy(line => line.ProductId)
            .Select(group => new ShippingSlipItem(group.Key, group.First().ProductName, group.Count()))
            .ToList();

        return ShippingSlip.Create(id, order.Id, customer.Id, customer.ShippingAddress, items, generatedAt);
    }
}
