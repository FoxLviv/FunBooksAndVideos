using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence;

/// <summary>
/// Process-wide, thread-safe storage of immutable snapshots. Reads and commits are serialised by one lock;
/// because snapshots are immutable, callers may use what they read without holding the lock.
/// Sequences are monotonic counters, like database sequences: gaps are possible and harmless.
/// </summary>
internal sealed class InMemoryDataStore
{
    private readonly Lock _gate = new();

    private long _customerSequence;
    private long _productSequence;
    private long _purchaseOrderSequence;
    private long _shippingSlipSequence;

    internal Dictionary<long, CustomerRecord> Customers { get; } = [];

    internal Dictionary<long, ProductRecord> Products { get; } = [];

    internal Dictionary<MembershipType, MembershipPlanRecord> MembershipPlans { get; } = [];

    internal Dictionary<long, PurchaseOrderRecord> PurchaseOrders { get; } = [];

    /// <summary>Shipping slips keyed by purchase order id: one slip per order, looked up in O(1).</summary>
    internal Dictionary<long, ShippingSlipRecord> ShippingSlipsByPurchaseOrder { get; } = [];

    /// <summary>Idempotency records keyed by the idempotency key (ordinal, case-sensitive).</summary>
    internal Dictionary<string, IdempotencyRecordRecord> IdempotencyRecords { get; } = new(StringComparer.Ordinal);

    public long NextCustomerId() => Interlocked.Increment(ref _customerSequence);

    public long NextProductId() => Interlocked.Increment(ref _productSequence);

    public long NextPurchaseOrderId() => Interlocked.Increment(ref _purchaseOrderSequence);

    public long NextShippingSlipId() => Interlocked.Increment(ref _shippingSlipSequence);

    /// <summary>Runs a query under the store lock and returns its (immutable) result.</summary>
    public TResult Read<TResult>(Func<InMemoryDataStore, TResult> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        lock (_gate)
        {
            return query(this);
        }
    }

    /// <summary>Validates every operation, then applies every operation. Either all of them are applied or none.</summary>
    public void Commit(IReadOnlyList<StoreOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);

        lock (_gate)
        {
            var touchedKeys = new HashSet<(string EntityName, object Key)>();
            foreach (var operation in operations)
            {
                operation.Validate(this, touchedKeys);
            }

            foreach (var operation in operations)
            {
                operation.Apply(this);
            }
        }
    }

    /// <summary>
    /// Adds reference data without overwriting what is already there (idempotent) and moves the sequences
    /// past the seeded identifiers so that generated ids never collide with them.
    /// </summary>
    public void Seed(
        IEnumerable<CustomerRecord> customers,
        IEnumerable<ProductRecord> products,
        IEnumerable<MembershipPlanRecord> membershipPlans,
        long firstPurchaseOrderId)
    {
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(membershipPlans);
        ArgumentOutOfRangeException.ThrowIfLessThan(firstPurchaseOrderId, 1);

        lock (_gate)
        {
            foreach (var customer in customers)
            {
                Customers.TryAdd(customer.Id, customer);
                EnsureAtLeast(ref _customerSequence, customer.Id);
            }

            foreach (var product in products)
            {
                Products.TryAdd(product.Id, product);
                EnsureAtLeast(ref _productSequence, product.Id);
            }

            foreach (var plan in membershipPlans)
            {
                MembershipPlans.TryAdd(plan.Type, plan);
            }

            EnsureAtLeast(ref _purchaseOrderSequence, firstPurchaseOrderId - 1);
        }
    }

    private static void EnsureAtLeast(ref long sequence, long value)
    {
        while (true)
        {
            var current = Volatile.Read(ref sequence);
            if (current >= value || Interlocked.CompareExchange(ref sequence, value, current) == current)
            {
                return;
            }
        }
    }
}
