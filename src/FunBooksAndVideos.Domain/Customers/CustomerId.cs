using System.Globalization;
using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.Customers;

/// <summary>Strongly typed identifier of a customer.</summary>
public readonly record struct CustomerId
{
    public CustomerId(long value)
    {
        if (value <= 0)
        {
            throw new DomainValidationException("customer_id.invalid", $"Customer id must be a positive number, but was {value}.");
        }

        Value = value;
    }

    public long Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
