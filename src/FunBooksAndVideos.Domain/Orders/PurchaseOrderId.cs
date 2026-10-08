using System.Globalization;
using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.Orders;

/// <summary>Strongly typed identifier of a purchase order.</summary>
public readonly record struct PurchaseOrderId
{
    public PurchaseOrderId(long value)
    {
        if (value <= 0)
        {
            throw new DomainValidationException("purchase_order_id.invalid", $"Purchase order id must be a positive number, but was {value}.");
        }

        Value = value;
    }

    public long Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
