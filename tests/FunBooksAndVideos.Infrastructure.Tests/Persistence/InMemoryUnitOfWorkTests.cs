using FluentAssertions;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Infrastructure.Tests.Persistence;

public sealed class InMemoryUnitOfWorkTests
{
    private readonly InMemoryDataStore _store = new();

    private StoreScope NewScope() => new(_store);

    private async Task<Customer> StoreCustomerAsync(long id)
    {
        var scope = NewScope();
        var customer = new CustomerBuilder().WithId(id).Build();
        scope.Customers.Add(customer);
        await scope.CommitAsync();
        return customer;
    }

    [Fact]
    public async Task Commit_with_nothing_pending_is_a_no_op()
    {
        var scope = NewScope();

        await scope.CommitAsync();

        scope.UnitOfWork.PendingOperationCount.Should().Be(0);
    }

    [Fact]
    public async Task Staged_changes_become_visible_only_after_commit()
    {
        var scope = NewScope();
        var customer = new CustomerBuilder().WithId(1).Build();

        scope.Customers.Add(customer);

        scope.UnitOfWork.PendingOperationCount.Should().Be(1);
        (await NewScope().Customers.FindAsync(customer.Id, CancellationToken.None)).Should().BeNull();

        await scope.CommitAsync();

        scope.UnitOfWork.PendingOperationCount.Should().Be(0);
        (await NewScope().Customers.FindAsync(customer.Id, CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task A_failing_commit_applies_nothing_from_the_batch()
    {
        var existing = await StoreCustomerAsync(1);
        var scope = NewScope();
        var fresh = new CustomerBuilder().WithId(2).Build();
        scope.Customers.Add(fresh);
        scope.Customers.Add(new CustomerBuilder().WithId(existing.Id.Value).WithName("Impostor").Build());

        var act = () => scope.CommitAsync();

        await act.Should().ThrowAsync<DuplicateEntityException>().Where(ex => ex.EntityName == "Customer" && (long)ex.Key == 1);
        (await NewScope().Customers.FindAsync(fresh.Id, CancellationToken.None)).Should().BeNull();
        (await NewScope().Customers.FindAsync(existing.Id, CancellationToken.None))!.Name.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task The_same_key_cannot_be_inserted_twice_in_one_batch()
    {
        var scope = NewScope();
        scope.Customers.Add(new CustomerBuilder().WithId(1).Build());
        scope.Customers.Add(new CustomerBuilder().WithId(1).Build());

        var act = () => scope.CommitAsync();

        await act.Should().ThrowAsync<DuplicateEntityException>();
        (await NewScope().Customers.FindAsync(new CustomerId(1), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task A_stale_update_is_rejected_as_a_concurrency_conflict()
    {
        await StoreCustomerAsync(1);
        var first = NewScope();
        var second = NewScope();
        var firstCopy = (await first.Customers.FindAsync(new CustomerId(1), CancellationToken.None))!;
        var secondCopy = (await second.Customers.FindAsync(new CustomerId(1), CancellationToken.None))!;

        firstCopy.ActivateMembership(MembershipType.BookClub, TestClock.Now);
        first.Customers.Update(firstCopy);
        await first.CommitAsync();

        secondCopy.ActivateMembership(MembershipType.VideoClub, TestClock.Now);
        second.Customers.Update(secondCopy);
        var act = () => second.CommitAsync();

        await act.Should().ThrowAsync<ConcurrencyConflictException>().Where(ex => ex.EntityName == "Customer");
        var stored = (await NewScope().Customers.FindAsync(new CustomerId(1), CancellationToken.None))!;
        stored.Version.Should().Be(1);
        stored.ActiveClubs.Should().Be(Club.Book);
    }

    [Fact]
    public async Task Updating_an_entity_that_was_never_stored_is_a_concurrency_conflict()
    {
        var scope = NewScope();
        scope.Customers.Update(new CustomerBuilder().WithId(99).Build());

        var act = () => scope.CommitAsync();

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Staging_the_same_update_twice_in_one_unit_of_work_is_a_programming_error()
    {
        await StoreCustomerAsync(1);
        var scope = NewScope();
        var customer = (await scope.Customers.FindAsync(new CustomerId(1), CancellationToken.None))!;
        scope.Customers.Update(customer);
        scope.Customers.Update(customer);

        var act = () => scope.CommitAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task A_failed_commit_discards_the_staged_changes_so_a_retry_starts_clean()
    {
        await StoreCustomerAsync(1);
        var scope = NewScope();
        scope.Customers.Add(new CustomerBuilder().WithId(1).Build());
        var failedCommit = () => scope.CommitAsync();
        await failedCommit.Should().ThrowAsync<DuplicateEntityException>();

        scope.UnitOfWork.PendingOperationCount.Should().Be(0);

        scope.Customers.Add(new CustomerBuilder().WithId(2).Build());
        await scope.CommitAsync();

        (await NewScope().Customers.FindAsync(new CustomerId(2), CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task A_cancelled_commit_applies_nothing_and_discards_the_staged_changes()
    {
        var scope = NewScope();
        scope.Customers.Add(new CustomerBuilder().WithId(1).Build());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = () => scope.UnitOfWork.CommitAsync(cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        scope.UnitOfWork.PendingOperationCount.Should().Be(0);
        (await NewScope().Customers.FindAsync(new CustomerId(1), CancellationToken.None)).Should().BeNull();

        await scope.UnitOfWork.CommitAsync(CancellationToken.None);

        (await NewScope().Customers.FindAsync(new CustomerId(1), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Commit_failures_are_reported_through_the_task_not_thrown_synchronously()
    {
        await StoreCustomerAsync(1);
        var scope = NewScope();
        scope.Customers.Add(new CustomerBuilder().WithId(1).Build());

        var commit = scope.UnitOfWork.CommitAsync(CancellationToken.None);

        commit.IsFaulted.Should().BeTrue();
        commit.Exception!.InnerException.Should().BeOfType<DuplicateEntityException>();
    }

    [Fact]
    public void Rejects_null_arguments()
    {
        var nullStore = () => new InMemoryUnitOfWork(null!);
        var nullOperation = () => NewScope().UnitOfWork.Enlist(null!);

        nullStore.Should().Throw<ArgumentNullException>();
        nullOperation.Should().Throw<ArgumentNullException>();
    }
}
