using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Catalog;

/// <summary>An online video.</summary>
public sealed class Video : DigitalProduct
{
    public Video(ProductId id, string name, Money price)
        : base(id, name, price)
    {
    }

    public override ProductKind Kind => ProductKind.Video;
}
