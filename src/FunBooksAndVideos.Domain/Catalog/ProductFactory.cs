using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Catalog;

/// <summary>
/// Simple (static) factory: the single place deciding which concrete <see cref="Product"/> corresponds to a
/// <see cref="ProductKind"/>. Adding a product type means adding a class and one case here.
/// </summary>
public static class ProductFactory
{
    public static Product Create(ProductKind kind, ProductId id, string name, Money price) =>
        kind switch
        {
            ProductKind.Book => new Book(id, name, price),
            ProductKind.Video => new Video(id, name, price),
            _ => throw new DomainValidationException("product.kind.unsupported", $"{kind} is not a supported product kind."),
        };
}
