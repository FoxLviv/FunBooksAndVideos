using System.Globalization;
using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.Shipping;

/// <summary>Strongly typed identifier of a shipping slip.</summary>
public readonly record struct ShippingSlipId
{
    public ShippingSlipId(long value)
    {
        if (value <= 0)
        {
            throw new DomainValidationException("shipping_slip_id.invalid", $"Shipping slip id must be a positive number, but was {value}.");
        }

        Value = value;
    }

    public long Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
