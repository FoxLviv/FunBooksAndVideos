using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Catalog;

/// <summary>A product that is delivered by post and therefore needs a shipping slip.</summary>
public abstract class PhysicalProduct : Product
{
    protected PhysicalProduct(ProductId id, string name, Money price)
        : base(id, name, price)
    {
    }

    public sealed override bool RequiresShipping => true;
}
