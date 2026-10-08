using FluentAssertions;
using FunBooksAndVideos.Domain.Idempotency;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Infrastructure.Tests.Persistence;

public sealed class InMemoryIdempotencyRecordRepositoryTests
{
    private static readonly IdempotencyKey Key = new("order-42");

    private readonly InMemoryDataStore _store = new();

    private StoreScope NewScope() => new(_store);

    private static IdempotencyRecord Record(string fingerprint = "abc", string payload = "{\"id\":1}") =>
        new(Key, fingerprint, payload, TestClock.Now);

    [Fact]
    public async Task Round_trips_a_committed_record()
    {
        var scope = NewScope();
        scope.IdempotencyRecords.Add(Record());
        await scope.CommitAsync();

        var loaded = await NewScope().IdempotencyRecords.FindAsync(Key, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Key.Should().Be(Key);
        loaded.RequestFingerprint.Should().Be("abc");
        loaded.ResponsePayload.Should().Be("{\"id\":1}");
        loaded.CreatedAt.Should().Be(TestClock.Now);
    }

    [Fact]
    public async Task Nothing_is_visible_before_the_commit_and_keys_are_case_sensitive()
    {
        var scope = NewScope();
        scope.IdempotencyRecords.Add(Record());

        (await scope.IdempotencyRecords.FindAsync(Key, CancellationToken.None)).Should().BeNull();

        await scope.CommitAsync();

        (await NewScope().IdempotencyRecords.FindAsync(new IdempotencyKey("ORDER-42"), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task A_duplicate_key_fails_the_commit_and_keeps_the_first_record()
    {
        var first = NewScope();
        first.IdempotencyRecords.Add(Record(fingerprint: "first"));
        await first.CommitAsync();

        var second = NewScope();
        second.IdempotencyRecords.Add(Record(fingerprint: "second"));
        var act = second.CommitAsync;

        var exception = (await act.Should().ThrowAsync<DuplicateEntityException>()).Which;
        exception.EntityName.Should().Be(IdempotencyRecord.EntityName);
        exception.Key.Should().Be(Key.Value);
        (await NewScope().IdempotencyRecords.FindAsync(Key, CancellationToken.None))!.RequestFingerprint.Should().Be("first");
    }

    [Fact]
    public async Task The_same_key_twice_in_one_unit_of_work_is_a_duplicate()
    {
        var scope = NewScope();
        scope.IdempotencyRecords.Add(Record());
        scope.IdempotencyRecords.Add(Record());

        var act = scope.CommitAsync;

        (await act.Should().ThrowAsync<DuplicateEntityException>()).Which.EntityName.Should().Be(IdempotencyRecord.EntityName);
        (await NewScope().IdempotencyRecords.FindAsync(Key, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task A_duplicate_key_rolls_back_the_order_staged_in_the_same_unit_of_work()
    {
        var first = NewScope();
        first.IdempotencyRecords.Add(Record());
        await first.CommitAsync();

        var second = NewScope();
        var order = new PurchaseOrderBuilder().WithBook().Build();
        second.PurchaseOrders.Add(order);
        second.IdempotencyRecords.Add(Record());
        var act = second.CommitAsync;

        await act.Should().ThrowAsync<DuplicateEntityException>();
        (await NewScope().PurchaseOrders.FindAsync(order.Id, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_for_an_unknown_or_default_key()
    {
        (await NewScope().IdempotencyRecords.FindAsync(Key, CancellationToken.None)).Should().BeNull();
        (await NewScope().IdempotencyRecords.FindAsync(default, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public void Rejects_null_records()
    {
        var act = () => NewScope().IdempotencyRecords.Add(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
