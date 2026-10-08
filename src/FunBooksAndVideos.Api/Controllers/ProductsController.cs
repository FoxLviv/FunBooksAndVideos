using System.Net.Mime;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Catalog.CreateProduct;
using FunBooksAndVideos.Application.Catalog.GetProduct;
using FunBooksAndVideos.Application.Catalog.ListProducts;
using FunBooksAndVideos.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FunBooksAndVideos.Api.Controllers;

/// <summary>Catalog of books and videos.</summary>
[ApiController]
[Route("api/v1/products")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.ProblemJson)]
public sealed class ProductsController : ControllerBase
{
    private readonly IQueryHandler<ListProductsQuery, IReadOnlyList<ProductDto>> _listProducts;
    private readonly IQueryHandler<GetProductQuery, ProductDto> _getProduct;
    private readonly ICommandHandler<CreateProductCommand, ProductDto> _createProduct;

    public ProductsController(
        IQueryHandler<ListProductsQuery, IReadOnlyList<ProductDto>> listProducts,
        IQueryHandler<GetProductQuery, ProductDto> getProduct,
        ICommandHandler<CreateProductCommand, ProductDto> createProduct)
    {
        ArgumentNullException.ThrowIfNull(listProducts);
        ArgumentNullException.ThrowIfNull(getProduct);
        ArgumentNullException.ThrowIfNull(createProduct);

        _listProducts = listProducts;
        _getProduct = getProduct;
        _createProduct = createProduct;
    }

    /// <summary>Lists all products.</summary>
    /// <response code="200">Products ordered by id.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> List(CancellationToken cancellationToken) =>
        Ok(await _listProducts.HandleAsync(new ListProductsQuery(), cancellationToken));

    /// <summary>Returns a product.</summary>
    /// <param name="id">Product id.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <response code="200">The product.</response>
    /// <response code="404">No such product.</response>
    [HttpGet("{id:long:min(1)}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ProductDto>> GetById(long id, CancellationToken cancellationToken) =>
        await _getProduct.HandleAsync(new GetProductQuery(id), cancellationToken);

    /// <summary>Adds a product to the catalog.</summary>
    /// <response code="201">Product created. The <c>Location</c> header points at the product.</response>
    /// <response code="400">Malformed request.</response>
    /// <response code="422">The data violates a domain rule (e.g. a price with more than two decimals).</response>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _createProduct.HandleAsync(request.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
