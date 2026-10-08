using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Mapping;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Application.Catalog.CreateProduct;

public sealed class CreateProductHandler : ICommandHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var price = Money.Of(command.Price);
        var id = await _products.NextIdentityAsync(cancellationToken);
        var product = ProductFactory.Create(command.Kind, id, command.Name, price);

        _products.Add(product);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ProductMapper.ToDto(product);
    }
}
