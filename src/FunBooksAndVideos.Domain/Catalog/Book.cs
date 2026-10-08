using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Catalog;

/// <summary>A printed book.</summary>
public sealed class Book : PhysicalProduct
{
    public Book(ProductId id, string name, Money price)
        : base(id, name, price)
    {
    }

    public override ProductKind Kind => ProductKind.Book;
}
