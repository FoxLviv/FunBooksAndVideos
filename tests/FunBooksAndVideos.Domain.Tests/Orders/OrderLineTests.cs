using FluentAssertions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.ValueObjects;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Domain.Tests.Orders;

public sealed class OrderLineTests
{
    [Fact]
    public void Product_line_snapshots_the_catalog_product()
    {
        var book = TestProducts.Book(id: 2, name: "The Girl on the train", price: 14.00m);

        var line = ProductLine.For(book);

        line.ProductId.Should().Be(new ProductId(2));
        line.ProductName.Should().Be("The Girl on the train");
        line.ProductKind.Should().Be(ProductKind.Book);
        line.RequiresShipping.Should().BeTrue();
        line.Price.Should().Be(Money.Of(14.00m));
        line.Description.Should().Be("Book \"The Girl on the train\"");
        line.ToString().Should().Be("Book \"The Girl on the train\" (14.00)");
    }

    [Fact]
    public void Product_line_for_a_video_does_not_require_shipping()
    {
        ProductLine.For(TestProducts.Video()).RequiresShipping.Should().BeFalse();
    }

    [Fact]
    public void Product_line_rejects_null_product_blank_name_and_unknown_kind()
    {
        var nullProduct = () => ProductLine.For(null!);
        var blankName = () => new ProductLine(new ProductId(1), " ", ProductKind.Book, true, Money.Zero);
        var unknownKind = () => new ProductLine(new ProductId(1), "Name", (ProductKind)9, true, Money.Zero);

        nullProduct.Should().Throw<ArgumentNullException>();
        blankName.Should().Throw<DomainValidationException>().Which.Code.Should().Be("product.name.invalid");
        unknownKind.Should().Throw<DomainValidationException>().Which.Code.Should().Be("product.kind.unsupported");
    }

    [Fact]
    public void Product_line_rejects_a_default_product_id()
    {
        var act = () => new ProductLine(default, "Name", ProductKind.Book, true, Money.Zero);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("order_line.product_id.invalid");
    }

    [Fact]
    public void Membership_line_takes_type_and_price_from_the_plan()
    {
        var line = MembershipLine.For(TestPlans.BookClub());

        line.MembershipType.Should().Be(MembershipType.BookClub);
        line.Price.Should().Be(Money.Of(TestPlans.BookClubPrice));
        line.Description.Should().Be("Book Club Membership");
    }

    [Fact]
    public void Membership_line_rejects_null_plan_and_unknown_type()
    {
        var nullPlan = () => MembershipLine.For(null!);
        var unknownType = () => new MembershipLine((MembershipType)9, Money.Zero);

        nullPlan.Should().Throw<ArgumentNullException>();
        unknownType.Should().Throw<DomainValidationException>().Which.Code.Should().Be("membership_type.unsupported");
    }

    [Fact]
    public void Visitor_dispatches_on_the_concrete_line_type()
    {
        var visitor = new DescribingVisitor();
        OrderLine product = ProductLine.For(TestProducts.Video());
        OrderLine membership = MembershipLine.For(TestPlans.Premium());

        product.Accept(visitor).Should().Be("product:1");
        membership.Accept(visitor).Should().Be("membership:Premium");
    }

    [Fact]
    public void Visitor_must_not_be_null()
    {
        var product = () => ProductLine.For(TestProducts.Video()).Accept<string>(null!);
        var membership = () => MembershipLine.For(TestPlans.Premium()).Accept<string>(null!);

        product.Should().Throw<ArgumentNullException>();
        membership.Should().Throw<ArgumentNullException>();
    }

    private sealed class DescribingVisitor : IOrderLineVisitor<string>
    {
        public string VisitProduct(ProductLine line) => $"product:{line.ProductId}";

        public string VisitMembership(MembershipLine line) => $"membership:{line.MembershipType}";
    }
}
