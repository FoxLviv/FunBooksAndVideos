using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Catalog;

namespace FunBooksAndVideos.Application.Mapping;

public static class ProductMapper
{
    public static ProductDto ToDto(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        return new ProductDto(product.Id.Value, product.Name, product.Kind, product.Price.Amount, product.RequiresShipping);
    }
}
