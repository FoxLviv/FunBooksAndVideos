using FluentAssertions;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Domain.ValueObjects;
using FunBooksAndVideos.TestKit;
using NSubstitute;

namespace FunBooksAndVideos.Application.Tests.PurchaseOrders;

public sealed class OrderLineFactoryTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IMembershipPlanRepository _plans = Substitute.For<IMembershipPlanRepository>();
    private readonly OrderLineFactory _factory;

    public OrderLineFactoryTests()
    {
        Product[] catalog = [TestProducts.Video(), TestProducts.Book()];

        _products.FindManyAsync(Arg.Any<IEnumerable<ProductId>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var ids = call.Arg<IEnumerable<ProductId>>().ToHashSet();
                IReadOnlyDictionary<ProductId, Product> found = catalog.Where(product => ids.Contains(product.Id)).ToDictionary(product => product.Id);
                return found;
            });

        _plans.FindAsync(Arg.Any<MembershipType>(), Arg.Any<CancellationToken>())
            .Returns(call => TestPlans.All().FirstOrDefault(plan => plan.Type == call.Arg<MembershipType>()));

        _factory = new OrderLineFactory(_products, _plans);
    }

    [Fact]
    public async Task Resolves_products_and_plans_from_the_catalog_preserving_order()
    {
        IReadOnlyList<OrderLineRequest> requests =
        [
            new ProductLineRequest(TestProducts.DefaultVideoId),
            new MembershipLineRequest(MembershipType.Premium),
            new ProductLineRequest(TestProducts.DefaultBookId),
        ];

        var lines = await _factory.CreateAsync(requests, CancellationToken.None);

        lines.Should().HaveCount(3);
        lines[0].Should().BeOfType<ProductLine>().Which.Should().BeEquivalentTo(new { ProductKind = ProductKind.Video, Price = Money.Of(19.50m), RequiresShipping = false });
        lines[1].Should().BeOfType<MembershipLine>().Which.Should().BeEquivalentTo(new { MembershipType = MembershipType.Premium, Price = Money.Of(TestPlans.PremiumPrice) });
        lines[2].Should().BeOfType<ProductLine>().Which.Should().BeEquivalentTo(new { ProductKind = ProductKind.Book, Price = Money.Of(14.00m), RequiresShipping = true });
    }

    [Fact]
    public async Task Reports_every_unknown_reference_at_once_keyed_by_position()
    {
        IReadOnlyList<OrderLineRequest> requests =
        [
            new ProductLineRequest(999),
            new MembershipLineRequest((MembershipType)42),
            new ProductLineRequest(TestProducts.DefaultBookId),
            new ProductLineRequest(1000),
        ];

        var act = () => _factory.CreateAsync(requests, CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Code.Should().Be(ValidationException.ErrorCode);
        exception.Errors.Keys.Should().BeEquivalentTo("lines[0].productId", "lines[1].membershipType", "lines[3].productId");
        exception.Errors["lines[0].productId"].Should().ContainSingle().Which.Should().Be("Product 999 does not exist.");
        exception.Errors["lines[1].membershipType"].Should().ContainSingle().Which.Should().Contain("not offered");
    }

    [Fact]
    public async Task Loads_products_once_using_distinct_ids()
    {
        IReadOnlyList<OrderLineRequest> requests =
        [
            new ProductLineRequest(TestProducts.DefaultBookId),
            new ProductLineRequest(TestProducts.DefaultBookId),
        ];

        var lines = await _factory.CreateAsync(requests, CancellationToken.None);

        lines.Should().HaveCount(2);
        await _products.Received(1).FindManyAsync(
            Arg.Is<IEnumerable<ProductId>>(ids => ids.Count() == 1 && ids.Single() == new ProductId(TestProducts.DefaultBookId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Skips_the_product_lookup_when_no_products_are_requested()
    {
        IReadOnlyList<OrderLineRequest> requests = [new MembershipLineRequest(MembershipType.BookClub)];

        var lines = await _factory.CreateAsync(requests, CancellationToken.None);

        lines.Should().ContainSingle().Which.Should().BeOfType<MembershipLine>();
        await _products.DidNotReceive().FindManyAsync(Arg.Any<IEnumerable<ProductId>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_empty_and_null_requests()
    {
        var empty = () => _factory.CreateAsync([], CancellationToken.None);
        var nullEntry = () => _factory.CreateAsync([null!], CancellationToken.None);
        var nullAmongValid = () => _factory.CreateAsync([new ProductLineRequest(TestProducts.DefaultBookId), null!], CancellationToken.None);
        var nullList = () => _factory.CreateAsync(null!, CancellationToken.None);

        (await empty.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("lines");
        (await nullEntry.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("lines");
        (await nullAmongValid.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("lines");
        await nullList.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Rejects_non_positive_product_ids()
    {
        var act = () => _factory.CreateAsync([new ProductLineRequest(0)], CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>().Where(ex => ex.Code == "product_id.invalid");
    }

    [Fact]
    public async Task Rejects_unknown_request_types()
    {
        var act = () => _factory.CreateAsync([new UnknownLineRequest()], CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public void Rejects_null_dependencies()
    {
        var nullProducts = () => new OrderLineFactory(null!, _plans);
        var nullPlans = () => new OrderLineFactory(_products, null!);

        nullProducts.Should().Throw<ArgumentNullException>();
        nullPlans.Should().Throw<ArgumentNullException>();
    }

    private sealed record UnknownLineRequest : OrderLineRequest;
}
