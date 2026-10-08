using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Catalog;

/// <summary>
/// Something the shop sells. The hierarchy below decides whether a product is physical
/// (<see cref="PhysicalProduct"/>) or digital (<see cref="DigitalProduct"/>), which drives
/// the shipping business rule without any type switching in the rules themselves.
/// </summary>
public abstract class Product : Entity<ProductId>
{
    public const int MaxNameLength = 200;

    protected Product(ProductId id, string name, Money price)
        : base(id)
    {
        Name = Guard.RequiredText(name, MaxNameLength, "product.name.invalid", "Product name");
        Price = price;
    }

    public string Name { get; }

    public Money Price { get; }

    public abstract ProductKind Kind { get; }

    /// <summary><see langword="true"/> when the product has to be physically delivered.</summary>
    public abstract bool RequiresShipping { get; }

    public override string ToString() => $"{Kind} \"{Name}\"";
}
