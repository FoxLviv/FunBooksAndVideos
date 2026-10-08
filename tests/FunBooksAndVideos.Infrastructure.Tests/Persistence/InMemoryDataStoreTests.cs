using FluentAssertions;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Mapping;
using FunBooksAndVideos.Infrastructure.Persistence.Records;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Infrastructure.Tests.Persistence;

public sealed class InMemoryDataStoreTests
{
    private readonly InMemoryDataStore _store = new();

    private static CustomerRecord Record(long id, string name = "Jane Doe", int version = 0) =>
        CustomerRecordMapper.ToRecord(new CustomerBuilder().WithId(id).WithName(name).Build(), version);

    [Fact]
    public void Sequences_start_at_one_and_increase()
    {
        _store.NextCustomerId().Should().Be(1);
        _store.NextCustomerId().Should().Be(2);
        _store.NextProductId().Should().Be(1);
        _store.NextPurchaseOrderId().Should().Be(1);
        _store.NextShippingSlipId().Should().Be(1);
    }

    [Fact]
    public void Seed_moves_sequences_past_the_seeded_identifiers()
    {
        _store.Seed([Record(4567890)], [ProductRecordMapper.ToRecord(TestProducts.Book(id: 4))], [], firstPurchaseOrderId: 3344656);

        _store.NextCustomerId().Should().Be(4567891);
        _store.NextProductId().Should().Be(5);
        _store.NextPurchaseOrderId().Should().Be(3344656);
    }

    [Fact]
    public void Seed_never_lowers_a_sequence_and_never_overwrites_existing_records()
    {
        _store.NextCustomerId().Should().Be(1);
        _store.NextCustomerId().Should().Be(2);
        _store.Seed([Record(1, "Original")], [], [], firstPurchaseOrderId: 1);
        _store.Seed([Record(1, "Overwrite attempt")], [], [], firstPurchaseOrderId: 1);

        _store.NextCustomerId().Should().Be(3);
        _store.Read(store => store.Customers[1].Name).Should().Be("Original");
    }

    [Fact]
    public void Commit_validates_every_operation_before_applying_any()
    {
        _store.Seed([Record(1)], [], [], firstPurchaseOrderId: 1);
        var insert = new InsertOperation<long, CustomerRecord>("Customer", store => store.Customers, 2, Record(2));
        var staleUpdate = new UpdateOperation<long, CustomerRecord>("Customer", store => store.Customers, 1, expectedVersion: 5, version => Record(1, "Changed", version));

        var act = () => _store.Commit([insert, staleUpdate]);

        act.Should().Throw<ConcurrencyConflictException>();
        _store.Read(store => store.Customers.ContainsKey(2)).Should().BeFalse();
        _store.Read(store => store.Customers[1].Name).Should().Be("Jane Doe");
    }

    [Fact]
    public void Commit_applies_updates_with_the_incremented_version()
    {
        _store.Seed([Record(1)], [], [], firstPurchaseOrderId: 1);
        var update = new UpdateOperation<long, CustomerRecord>("Customer", store => store.Customers, 1, expectedVersion: 0, version => Record(1, "Changed", version));

        _store.Commit([update]);

        _store.Read(store => store.Customers[1]).Should().BeEquivalentTo(new { Name = "Changed", Version = 1 });
    }

    [Fact]
    public void Operations_validate_their_arguments()
    {
        var blankEntity = () => new InsertOperation<long, CustomerRecord>(" ", store => store.Customers, 1, Record(1));
        var nullTable = () => new InsertOperation<long, CustomerRecord>("Customer", null!, 1, Record(1));
        var nullRecord = () => new InsertOperation<long, CustomerRecord>("Customer", store => store.Customers, 1, null!);
        var negativeVersion = () => new UpdateOperation<long, CustomerRecord>("Customer", store => store.Customers, 1, -1, version => Record(1, version: version));
        var nullFactory = () => new UpdateOperation<long, CustomerRecord>("Customer", store => store.Customers, 1, 0, null!);

        blankEntity.Should().Throw<ArgumentException>();
        nullTable.Should().Throw<ArgumentNullException>();
        nullRecord.Should().Throw<ArgumentNullException>();
        negativeVersion.Should().Throw<ArgumentOutOfRangeException>();
        nullFactory.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Store_validates_its_arguments()
    {
        var nullQuery = () => _store.Read<int>(null!);
        var nullOperations = () => _store.Commit(null!);
        var nullCustomers = () => _store.Seed(null!, [], [], 1);
        var badFirstOrderId = () => _store.Seed([], [], [], 0);

        nullQuery.Should().Throw<ArgumentNullException>();
        nullOperations.Should().Throw<ArgumentNullException>();
        nullCustomers.Should().Throw<ArgumentNullException>();
        badFirstOrderId.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Concurrent_commits_never_lose_an_insert()
    {
        var tasks = Enumerable.Range(1, 200).Select(id => Task.Run(() =>
            _store.Commit([new InsertOperation<long, CustomerRecord>("Customer", store => store.Customers, id, Record(id))])));

        await Task.WhenAll(tasks);

        _store.Read(store => store.Customers.Count).Should().Be(200);
    }

    [Fact]
    public async Task Concurrent_sequence_calls_never_produce_duplicates()
    {
        var ids = new System.Collections.Concurrent.ConcurrentBag<long>();

        await Task.WhenAll(Enumerable.Range(0, 500).Select(_ => Task.Run(() => ids.Add(_store.NextPurchaseOrderId()))));

        ids.Should().OnlyHaveUniqueItems().And.HaveCount(500);
    }

    [Fact]
    public void Membership_plans_are_keyed_by_type()
    {
        _store.Seed([], [], [MembershipPlanRecordMapper.ToRecord(TestPlans.Premium())], 1);

        _store.Read(store => store.MembershipPlans[MembershipType.Premium].Price.Amount).Should().Be(30.00m);
    }
}
