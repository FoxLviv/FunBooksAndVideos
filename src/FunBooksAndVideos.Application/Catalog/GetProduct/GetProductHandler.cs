using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Application.Mapping;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Persistence;

namespace FunBooksAndVideos.Application.Catalog.GetProduct;

public sealed class GetProductHandler : IQueryHandler<GetProductQuery, ProductDto>
{
    private readonly IProductRepository _products;

    public GetProductHandler(IProductRepository products)
    {
        ArgumentNullException.ThrowIfNull(products);
        _products = products;
    }

    public async Task<ProductDto> HandleAsync(GetProductQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var product = await _products.FindAsync(new ProductId(query.ProductId), cancellationToken)
            ?? throw new NotFoundException("product.not_found", $"Product {query.ProductId} was not found.");

        return ProductMapper.ToDto(product);
    }
}
