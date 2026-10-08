using FluentAssertions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Domain.Tests.Shipping;

public sealed class ShippingSlipFactoryTests
{
    private static readonly ShippingSlipId SlipId = new(1);

    [Fact]
    public void Creates_a_slip_with_the_physical_products_only()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithKataExampleLines().Build();

        var slip = ShippingSlipFactory.CreateFor(SlipId, order, customer, TestClock.Now);

        slip.Id.Should().Be(SlipId);
        slip.PurchaseOrderId.Should().Be(order.Id);
        slip.CustomerId.Should().Be(customer.Id);
        slip.Address.Should().Be(TestAddresses.London());
        slip.GeneratedAt.Should().Be(TestClock.Now);
        slip.Version.Should().Be(0);
        slip.Items.Should().ContainSingle().Which.Should().Be(new ShippingSlipItem(new ProductId(TestProducts.DefaultBookId), "The Girl on the train", 1));
    }

    [Fact]
    public void Groups_repeated_products_into_quantities()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder()
            .ForCustomer(customer)
            .WithBook(id: 2)
            .WithBook(id: 3)
            .WithBook(id: 2)
            .WithVideo()
            .Build();

        var slip = ShippingSlipFactory.CreateFor(SlipId, order, customer, TestClock.Now);

        slip.Items.Should().HaveCount(2);
        slip.Items.Should().ContainSingle(item => item.ProductId == new ProductId(2)).Which.Quantity.Should().Be(2);
        slip.Items.Should().ContainSingle(item => item.ProductId == new ProductId(3)).Which.Quantity.Should().Be(1);
    }

    [Fact]
    public void Fails_when_the_customer_has_no_shipping_address()
    {
        var customer = new CustomerBuilder().WithoutShippingAddress().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithBook().Build();

        var act = () => ShippingSlipFactory.CreateFor(SlipId, order, customer, TestClock.Now);

        act.Should().Throw<BusinessRuleViolationException>().Which.Code.Should().Be("customer.shipping_address.missing");
    }

    [Fact]
    public void Fails_when_there_is_nothing_to_ship()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithVideo().WithMembership(MembershipType.BookClub).Build();

        var act = () => ShippingSlipFactory.CreateFor(SlipId, order, customer, TestClock.Now);

        act.Should().Throw<BusinessRuleViolationException>().Which.Code.Should().Be("shipping_slip.nothing_to_ship");
    }

    [Fact]
    public void Fails_when_the_order_belongs_to_another_customer()
    {
        var customer = new CustomerBuilder().WithId(1).Build();
        var order = new PurchaseOrderBuilder().ForCustomer(2).WithBook().Build();

        var act = () => ShippingSlipFactory.CreateFor(SlipId, order, customer, TestClock.Now);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_slip.customer.mismatch");
    }

    [Fact]
    public void Rejects_null_arguments()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().WithBook().Build();

        var nullOrder = () => ShippingSlipFactory.CreateFor(SlipId, null!, customer, TestClock.Now);
        var nullCustomer = () => ShippingSlipFactory.CreateFor(SlipId, order, null!, TestClock.Now);

        nullOrder.Should().Throw<ArgumentNullException>();
        nullCustomer.Should().Throw<ArgumentNullException>();
    }
}
