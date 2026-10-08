using FunBooksAndVideos.Domain.Catalog;

namespace FunBooksAndVideos.Application.Dtos;

/// <summary>Catalog product.</summary>
/// <param name="Id">Product id.</param>
/// <param name="Name">Title.</param>
/// <param name="Kind">Book or video.</param>
/// <param name="Price">Unit price.</param>
/// <param name="RequiresShipping">Whether buying it produces a shipping slip.</param>
public sealed record ProductDto(long Id, string Name, ProductKind Kind, decimal Price, bool RequiresShipping);
