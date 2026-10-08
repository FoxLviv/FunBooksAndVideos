using FluentAssertions;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Infrastructure.Tests.Persistence;

public sealed class InMemoryPurchaseOrderRepositoryTests
{
    private readonly InMemoryDataStore _store = new();

    private StoreScope NewScope() => new(_store);

    [Fact]
    public async Task Round_trips_a_processed_order_with_all_its_lines()
    {
        var scope = NewScope();
        var order = new PurchaseOrderBuilder().WithKataExampleLines().Build();
        order.MarkProcessed(TestClock.Now.AddSeconds(1));
        scope.PurchaseOrders.Add(order);
        await scope.CommitAsync();

        var loaded = await NewScope().PurchaseOrders.FindAsync(order.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Should().NotBeSameAs(order);
        loaded.CustomerId.Should().Be(order.CustomerId);
        loaded.Status.Should().Be(PurchaseOrderStatus.Processed);
        loaded.CreatedAt.Should().Be(TestClock.Now);
        loaded.ProcessedAt.Should().Be(TestClock.Now.AddSeconds(1));
        loaded.Total.Should().Be(order.Total);
        loaded.Lines.Select(line => line.Description).Should().Equal(order.Lines.Select(line => line.Description));
        loaded.Lines.Select(line => line.Price).Should().Equal(order.Lines.Select(line => line.Price));
        loaded.Version.Should().Be(0);
    }

    [Fact]
    public async Task Find_returns_null_for_unknown_orders()
    {
        (await NewScope().PurchaseOrders.FindAsync(new PurchaseOrderId(1), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Duplicate_order_ids_are_rejected_at_commit()
    {
        var first = NewScope();
        first.PurchaseOrders.Add(new PurchaseOrderBuilder().WithId(10).Build());
        await first.CommitAsync();

        var second = NewScope();
        second.PurchaseOrders.Add(new PurchaseOrderBuilder().WithId(10).Build());
        var act = () => second.CommitAsync();

        await act.Should().ThrowAsync<DuplicateEntityException>().Where(ex => ex.EntityName == "Purchase order");
    }

    [Fact]
    public async Task Identities_come_from_the_order_sequence()
    {
        _store.Seed([], [], [], firstPurchaseOrderId: 3344656);
        var scope = NewScope();

        var id = await scope.PurchaseOrders.NextIdentityAsync(CancellationToken.None);

        id.Value.Should().Be(3344656);
    }

    [Fact]
    public void Rejects_null_orders()
    {
        var act = () => NewScope().PurchaseOrders.Add(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
