using FluentAssertions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Domain.Tests.Shipping;

public sealed class ShippingSlipTests
{
    private static readonly ShippingSlipId SlipId = new(1);
    private static readonly PurchaseOrderId OrderId = new(3344656);
    private static readonly CustomerId CustomerId = new(4567890);

    private static ShippingSlipItem Item(long productId = 2, int quantity = 1) =>
        new(new ProductId(productId), "The Girl on the train", quantity);

    [Fact]
    public void Create_requires_at_least_one_item()
    {
        var act = () => ShippingSlip.Create(SlipId, OrderId, CustomerId, TestAddresses.London(), [], TestClock.Now);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip.items.empty");
    }

    [Fact]
    public void Create_rejects_null_items()
    {
        var nullCollection = () => ShippingSlip.Create(SlipId, OrderId, CustomerId, TestAddresses.London(), null!, TestClock.Now);
        var nullEntry = () => ShippingSlip.Create(SlipId, OrderId, CustomerId, TestAddresses.London(), [null!], TestClock.Now);
        var nullAmongValid = () => ShippingSlip.Create(SlipId, OrderId, CustomerId, TestAddresses.London(), [Item(), null!], TestClock.Now);

        nullCollection.Should().Throw<ArgumentNullException>();
        nullEntry.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip.items.invalid");
        nullAmongValid.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip.items.invalid");
    }

    [Fact]
    public void Create_rejects_the_same_product_twice()
    {
        var act = () => ShippingSlip.Create(SlipId, OrderId, CustomerId, TestAddresses.London(), [Item(2), Item(2)], TestClock.Now);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip.items.duplicate");
    }

    [Fact]
    public void Create_requires_an_address()
    {
        var act = () => ShippingSlip.Create(SlipId, OrderId, CustomerId, null!, [Item()], TestClock.Now);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip.address.missing");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Items_require_a_positive_quantity(int quantity)
    {
        var act = () => Item(quantity: quantity);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip.item.quantity.invalid");
    }

    [Fact]
    public void Items_require_a_product_name()
    {
        var act = () => new ShippingSlipItem(new ProductId(1), "", 1);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("product.name.invalid");
    }

    [Fact]
    public void Create_rejects_a_default_purchase_order_id()
    {
        var act = () => ShippingSlip.Create(SlipId, default, CustomerId, TestAddresses.London(), [Item()], TestClock.Now);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip.purchase_order_id.invalid");
    }

    [Fact]
    public void Create_rejects_a_default_customer_id()
    {
        var act = () => ShippingSlip.Create(SlipId, OrderId, default, TestAddresses.London(), [Item()], TestClock.Now);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip.customer_id.invalid");
    }

    [Fact]
    public void Rehydrate_rejects_a_default_slip_id()
    {
        var act = () => ShippingSlip.Rehydrate(default, OrderId, CustomerId, TestAddresses.London(), [Item()], TestClock.Now, version: 1);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("entity.id.invalid");
    }

    [Fact]
    public void Items_reject_a_default_product_id()
    {
        var act = () => new ShippingSlipItem(default, "The Girl on the train", 1);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip.item.product_id.invalid");
    }

    [Fact]
    public void Rehydrate_restores_the_version()
    {
        var slip = ShippingSlip.Rehydrate(SlipId, OrderId, CustomerId, TestAddresses.London(), [Item()], TestClock.Now, version: 4);

        slip.Version.Should().Be(4);
        slip.Items.Should().ContainSingle();
        slip.Items.Should().NotBeAssignableTo<List<ShippingSlipItem>>();
    }

    [Fact]
    public void Shipping_slip_ids_must_be_positive()
    {
        var act = () => new ShippingSlipId(0);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip_id.invalid");
        new ShippingSlipId(12).ToString().Should().Be("12");
    }
}
