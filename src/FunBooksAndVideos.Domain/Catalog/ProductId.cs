using System.Globalization;
using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.Catalog;

/// <summary>Strongly typed identifier of a catalog product.</summary>
public readonly record struct ProductId
{
    public ProductId(long value)
    {
        if (value <= 0)
        {
            throw new DomainValidationException("product_id.invalid", $"Product id must be a positive number, but was {value}.");
        }

        Value = value;
    }

    public long Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
