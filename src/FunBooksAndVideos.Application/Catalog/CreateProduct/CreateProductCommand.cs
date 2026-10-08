using FunBooksAndVideos.Domain.Catalog;

namespace FunBooksAndVideos.Application.Catalog.CreateProduct;

/// <summary>Adds a product to the catalog.</summary>
/// <param name="Kind">Book or video.</param>
/// <param name="Name">Title.</param>
/// <param name="Price">Unit price, at most two decimals.</param>
public sealed record CreateProductCommand(ProductKind Kind, string Name, decimal Price);
