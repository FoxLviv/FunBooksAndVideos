using FluentAssertions;
using FunBooksAndVideos.Application.Catalog.CreateProduct;
using FunBooksAndVideos.Application.Catalog.GetProduct;
using FunBooksAndVideos.Application.Catalog.ListMembershipPlans;
using FunBooksAndVideos.Application.Catalog.ListProducts;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.TestKit;
using NSubstitute;

namespace FunBooksAndVideos.Application.Tests.Catalog;

public sealed class CatalogHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IMembershipPlanRepository _plans = Substitute.For<IMembershipPlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public CatalogHandlerTests()
    {
        _products.NextIdentityAsync(Arg.Any<CancellationToken>()).Returns(new ProductId(9));
    }

    [Theory]
    [InlineData(ProductKind.Book, typeof(Book), true)]
    [InlineData(ProductKind.Video, typeof(Video), false)]
    public async Task CreateProduct_uses_the_factory_and_commits(ProductKind kind, Type expectedType, bool requiresShipping)
    {
        var handler = new CreateProductHandler(_products, _unitOfWork);

        var dto = await handler.HandleAsync(new CreateProductCommand(kind, " Title ", 42.00m), CancellationToken.None);

        dto.Should().Be(new ProductDto(9, "Title", kind, 42.00m, requiresShipping));
        _products.Received(1).Add(Arg.Is<Product>(product => product.GetType() == expectedType && product.Id.Value == 9));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(-1, "money.negative")]
    [InlineData(1.999, "money.precision")]
    public async Task CreateProduct_validates_the_price_before_allocating_an_id(decimal price, string expectedCode)
    {
        var handler = new CreateProductHandler(_products, _unitOfWork);

        var act = () => handler.HandleAsync(new CreateProductCommand(ProductKind.Book, "Title", price), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>().Where(ex => ex.Code == expectedCode);
        await _products.DidNotReceive().NextIdentityAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProduct_rejects_unknown_kinds()
    {
        var handler = new CreateProductHandler(_products, _unitOfWork);

        var act = () => handler.HandleAsync(new CreateProductCommand((ProductKind)7, "Title", 1m), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>().Where(ex => ex.Code == "product.kind.unsupported");
        _products.DidNotReceive().Add(Arg.Any<Product>());
    }

    [Fact]
    public async Task GetProduct_maps_the_product_or_reports_not_found()
    {
        var book = TestProducts.Book();
        _products.FindAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        var handler = new GetProductHandler(_products);

        var dto = await handler.HandleAsync(new GetProductQuery(book.Id.Value), CancellationToken.None);
        var missing = () => handler.HandleAsync(new GetProductQuery(book.Id.Value + 100), CancellationToken.None);

        dto.Should().Be(new ProductDto(book.Id.Value, book.Name, ProductKind.Book, 14.00m, true));
        await missing.Should().ThrowAsync<NotFoundException>().Where(ex => ex.Code == "product.not_found");
    }

    [Fact]
    public async Task ListProducts_maps_every_product()
    {
        _products.ListAsync(Arg.Any<CancellationToken>()).Returns([TestProducts.Video(), TestProducts.Book()]);

        var dtos = await new ListProductsHandler(_products).HandleAsync(new ListProductsQuery(), CancellationToken.None);

        dtos.Select(dto => dto.Kind).Should().Equal(ProductKind.Video, ProductKind.Book);
    }

    [Fact]
    public async Task ListMembershipPlans_maps_prices_and_clubs()
    {
        _plans.ListAsync(Arg.Any<CancellationToken>()).Returns(TestPlans.All());

        var dtos = await new ListMembershipPlansHandler(_plans).HandleAsync(new ListMembershipPlansQuery(), CancellationToken.None);

        dtos.Should().HaveCount(3);
        dtos.Single(dto => dto.Type == MembershipType.Premium).Should().BeEquivalentTo(
            new MembershipPlanDto(MembershipType.Premium, "Premium Membership", TestPlans.PremiumPrice, [Club.Book, Club.Video]));
        dtos.Single(dto => dto.Type == MembershipType.BookClub).Clubs.Should().Equal(Club.Book);
    }

    [Fact]
    public async Task Handlers_reject_null_arguments()
    {
        var nullProducts = () => new CreateProductHandler(null!, _unitOfWork);
        var nullUnitOfWork = () => new CreateProductHandler(_products, null!);
        var nullGet = () => new GetProductHandler(null!);
        var nullList = () => new ListProductsHandler(null!);
        var nullPlans = () => new ListMembershipPlansHandler(null!);
        var nullCommand = () => new CreateProductHandler(_products, _unitOfWork).HandleAsync(null!, CancellationToken.None);
        var nullQuery = () => new GetProductHandler(_products).HandleAsync(null!, CancellationToken.None);
        var nullListQuery = () => new ListProductsHandler(_products).HandleAsync(null!, CancellationToken.None);
        var nullPlansQuery = () => new ListMembershipPlansHandler(_plans).HandleAsync(null!, CancellationToken.None);

        nullProducts.Should().Throw<ArgumentNullException>();
        nullUnitOfWork.Should().Throw<ArgumentNullException>();
        nullGet.Should().Throw<ArgumentNullException>();
        nullList.Should().Throw<ArgumentNullException>();
        nullPlans.Should().Throw<ArgumentNullException>();
        await nullCommand.Should().ThrowAsync<ArgumentNullException>();
        await nullQuery.Should().ThrowAsync<ArgumentNullException>();
        await nullListQuery.Should().ThrowAsync<ArgumentNullException>();
        await nullPlansQuery.Should().ThrowAsync<ArgumentNullException>();
    }
}
