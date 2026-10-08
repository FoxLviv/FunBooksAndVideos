using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Catalog;

/// <summary>A product consumed online; nothing is shipped.</summary>
public abstract class DigitalProduct : Product
{
    protected DigitalProduct(ProductId id, string name, Money price)
        : base(id, name, price)
    {
    }

    public sealed override bool RequiresShipping => false;
}
