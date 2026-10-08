using FluentAssertions;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Infrastructure.Tests.Persistence;

public sealed class InMemoryShippingSlipRepositoryTests
{
    private readonly InMemoryDataStore _store = new();

    private StoreScope NewScope() => new(_store);

    [Fact]
    public async Task Finds_the_slip_of_a_purchase_order()
    {
        var scope = NewScope();
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithBook().WithBook().Build();
        var id = await scope.ShippingSlips.NextIdentityAsync(CancellationToken.None);
        var slip = ShippingSlipFactory.CreateFor(id, order, customer, TestClock.Now);
        scope.ShippingSlips.Add(slip);
        await scope.CommitAsync();

        var loaded = await NewScope().ShippingSlips.FindByPurchaseOrderAsync(order.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Should().NotBeSameAs(slip);
        loaded.Id.Should().Be(id);
        loaded.CustomerId.Should().Be(customer.Id);
        loaded.Address.Should().Be(TestAddresses.London());
        loaded.Items.Should().Equal(slip.Items);
        loaded.GeneratedAt.Should().Be(TestClock.Now);
        loaded.Version.Should().Be(0);
    }

    [Fact]
    public async Task Returns_null_when_the_order_has_no_slip()
    {
        (await NewScope().ShippingSlips.FindByPurchaseOrderAsync(new PurchaseOrderId(5), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public void Rejects_null_slips()
    {
        var act = () => NewScope().ShippingSlips.Add(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task A_purchase_order_can_have_only_one_shipping_slip()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithBook().Build();
        var first = NewScope();
        first.ShippingSlips.Add(ShippingSlipFactory.CreateFor(new ShippingSlipId(1), order, customer, TestClock.Now));
        await first.CommitAsync();

        var second = NewScope();
        second.ShippingSlips.Add(ShippingSlipFactory.CreateFor(new ShippingSlipId(2), order, customer, TestClock.Now));
        var act = () => second.CommitAsync();

        await act.Should().ThrowAsync<Domain.Persistence.DuplicateEntityException>().Where(ex => (long)ex.Key == order.Id.Value);
        (await NewScope().ShippingSlips.FindByPurchaseOrderAsync(order.Id, CancellationToken.None))!.Id.Should().Be(new ShippingSlipId(1));
    }
}
