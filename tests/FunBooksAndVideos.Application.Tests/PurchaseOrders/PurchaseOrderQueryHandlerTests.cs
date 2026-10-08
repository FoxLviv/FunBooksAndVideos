using FluentAssertions;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Application.PurchaseOrders.GetPurchaseOrder;
using FunBooksAndVideos.Application.PurchaseOrders.GetShippingSlip;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.TestKit;
using NSubstitute;

namespace FunBooksAndVideos.Application.Tests.PurchaseOrders;

public sealed class PurchaseOrderQueryHandlerTests
{
    private readonly IPurchaseOrderRepository _orders = Substitute.For<IPurchaseOrderRepository>();
    private readonly IShippingSlipRepository _slips = Substitute.For<IShippingSlipRepository>();

    [Fact]
    public async Task GetPurchaseOrder_maps_the_order()
    {
        var order = new PurchaseOrderBuilder().WithKataExampleLines().Build();
        _orders.FindAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var dto = await new GetPurchaseOrderHandler(_orders).HandleAsync(new GetPurchaseOrderQuery(order.Id.Value), CancellationToken.None);

        dto.Id.Should().Be(order.Id.Value);
        dto.Total.Should().Be(48.50m);
        dto.Status.Should().Be(PurchaseOrderStatus.Pending);
        dto.Lines.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPurchaseOrder_reports_unknown_orders()
    {
        var act = () => new GetPurchaseOrderHandler(_orders).HandleAsync(new GetPurchaseOrderQuery(42), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().Where(ex => ex.Code == "purchase_order.not_found");
    }

    [Fact]
    public async Task GetShippingSlip_maps_the_slip_of_the_order()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithBook().Build();
        var slip = ShippingSlipFactory.CreateFor(new ShippingSlipId(3), order, customer, TestClock.Now);
        _orders.FindAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _slips.FindByPurchaseOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(slip);

        var dto = await new GetShippingSlipHandler(_orders, _slips).HandleAsync(new GetShippingSlipQuery(order.Id.Value), CancellationToken.None);

        dto.Id.Should().Be(3);
        dto.PurchaseOrderId.Should().Be(order.Id.Value);
        dto.CustomerId.Should().Be(customer.Id.Value);
        dto.Address.City.Should().Be("London");
        dto.Items.Should().ContainSingle().Which.Quantity.Should().Be(1);
        dto.GeneratedAt.Should().Be(TestClock.Now);
    }

    [Fact]
    public async Task GetShippingSlip_distinguishes_unknown_order_from_missing_slip()
    {
        var order = new PurchaseOrderBuilder().WithVideo().Build();
        _orders.FindAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var handler = new GetShippingSlipHandler(_orders, _slips);

        var unknownOrder = () => handler.HandleAsync(new GetShippingSlipQuery(order.Id.Value + 1), CancellationToken.None);
        var noSlip = () => handler.HandleAsync(new GetShippingSlipQuery(order.Id.Value), CancellationToken.None);

        await unknownOrder.Should().ThrowAsync<NotFoundException>().Where(ex => ex.Code == "purchase_order.not_found");
        await noSlip.Should().ThrowAsync<NotFoundException>().Where(ex => ex.Code == "shipping_slip.not_found");
    }

    [Fact]
    public async Task Handlers_reject_null_queries_and_dependencies()
    {
        var nullOrders = () => new GetPurchaseOrderHandler(null!);
        var nullSlipOrders = () => new GetShippingSlipHandler(null!, _slips);
        var nullSlips = () => new GetShippingSlipHandler(_orders, null!);
        var nullQuery = () => new GetPurchaseOrderHandler(_orders).HandleAsync(null!, CancellationToken.None);
        var nullSlipQuery = () => new GetShippingSlipHandler(_orders, _slips).HandleAsync(null!, CancellationToken.None);

        nullOrders.Should().Throw<ArgumentNullException>();
        nullSlipOrders.Should().Throw<ArgumentNullException>();
        nullSlips.Should().Throw<ArgumentNullException>();
        await nullQuery.Should().ThrowAsync<ArgumentNullException>();
        await nullSlipQuery.Should().ThrowAsync<ArgumentNullException>();
    }
}
