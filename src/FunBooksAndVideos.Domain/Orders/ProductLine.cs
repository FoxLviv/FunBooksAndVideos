using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Orders;

/// <summary>A purchased product, captured as a snapshot of the catalog entry at purchase time.</summary>
public sealed class ProductLine : OrderLine
{
    public ProductLine(ProductId productId, string productName, ProductKind productKind, bool requiresShipping, Money price)
        : base(price)
    {
        ProductId = Guard.NotDefault(productId, "order_line.product_id.invalid", "Product id");
        ProductName = Guard.RequiredText(productName, Product.MaxNameLength, "product.name.invalid", "Product name");
        ProductKind = Guard.DefinedEnum(productKind, "product.kind.unsupported");
        RequiresShipping = requiresShipping;
    }

    public ProductId ProductId { get; }

    public string ProductName { get; }

    public ProductKind ProductKind { get; }

    public bool RequiresShipping { get; }

    public override string Description => $"{ProductKind} \"{ProductName}\"";

    /// <summary>Snapshots the given catalog product.</summary>
    public static ProductLine For(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        return new ProductLine(product.Id, product.Name, product.Kind, product.RequiresShipping, product.Price);
    }

    public override TResult Accept<TResult>(IOrderLineVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.VisitProduct(this);
    }
}
