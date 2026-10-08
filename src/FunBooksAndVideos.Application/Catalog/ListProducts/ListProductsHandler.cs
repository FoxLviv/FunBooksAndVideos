using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Mapping;
using FunBooksAndVideos.Domain.Persistence;

namespace FunBooksAndVideos.Application.Catalog.ListProducts;

public sealed class ListProductsHandler : IQueryHandler<ListProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _products;

    public ListProductsHandler(IProductRepository products)
    {
        ArgumentNullException.ThrowIfNull(products);
        _products = products;
    }

    public async Task<IReadOnlyList<ProductDto>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = await _products.ListAsync(cancellationToken);
        return products.Select(ProductMapper.ToDto).ToList();
    }
}
