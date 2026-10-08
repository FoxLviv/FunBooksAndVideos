using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.Shipping;

/// <summary>A product to pack, with the number of copies.</summary>
public sealed record ShippingSlipItem
{
    public ShippingSlipItem(ProductId productId, string productName, int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("shipping_slip.item.quantity.invalid", $"Quantity must be positive, but was {quantity}.");
        }

        ProductId = Guard.NotDefault(productId, "shipping_slip.item.product_id.invalid", "Product id");
        ProductName = Guard.RequiredText(productName, Product.MaxNameLength, "product.name.invalid", "Product name");
        Quantity = quantity;
    }

    public ProductId ProductId { get; }

    public string ProductName { get; }

    public int Quantity { get; }
}
