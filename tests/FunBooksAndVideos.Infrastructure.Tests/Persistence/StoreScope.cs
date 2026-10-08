using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Repositories;

namespace FunBooksAndVideos.Infrastructure.Tests.Persistence;

/// <summary>Mimics one DI scope (request): one unit of work and repositories bound to it, over a shared store.</summary>
internal sealed class StoreScope
{
    public StoreScope(InMemoryDataStore store)
    {
        Store = store;
        UnitOfWork = new InMemoryUnitOfWork(store);
        Customers = new InMemoryCustomerRepository(store, UnitOfWork);
        Products = new InMemoryProductRepository(store, UnitOfWork);
        MembershipPlans = new InMemoryMembershipPlanRepository(store);
        PurchaseOrders = new InMemoryPurchaseOrderRepository(store, UnitOfWork);
        ShippingSlips = new InMemoryShippingSlipRepository(store, UnitOfWork);
        IdempotencyRecords = new InMemoryIdempotencyRecordRepository(store, UnitOfWork);
    }

    public InMemoryIdempotencyRecordRepository IdempotencyRecords { get; }

    public InMemoryDataStore Store { get; }

    public InMemoryUnitOfWork UnitOfWork { get; }

    public InMemoryCustomerRepository Customers { get; }

    public InMemoryProductRepository Products { get; }

    public InMemoryMembershipPlanRepository MembershipPlans { get; }

    public InMemoryPurchaseOrderRepository PurchaseOrders { get; }

    public InMemoryShippingSlipRepository ShippingSlips { get; }

    public Task CommitAsync() => UnitOfWork.CommitAsync(CancellationToken.None);
}
