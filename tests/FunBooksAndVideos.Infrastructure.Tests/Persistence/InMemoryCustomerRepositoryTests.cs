using FluentAssertions;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Repositories;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Infrastructure.Tests.Persistence;

public sealed class InMemoryCustomerRepositoryTests
{
    private readonly InMemoryDataStore _store = new();

    private StoreScope NewScope() => new(_store);

    [Fact]
    public async Task Find_returns_null_for_unknown_customers()
    {
        (await NewScope().Customers.FindAsync(new CustomerId(123), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Round_trips_a_customer_with_address_and_memberships()
    {
        var scope = NewScope();
        var customer = new CustomerBuilder().WithId(7).WithName("Ada").WithMembership(MembershipType.Premium, TestClock.Now).Build();
        scope.Customers.Add(customer);
        await scope.CommitAsync();

        var loaded = await NewScope().Customers.FindAsync(customer.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Should().NotBeSameAs(customer);
        loaded.Id.Should().Be(customer.Id);
        loaded.Name.Should().Be("Ada");
        loaded.ShippingAddress.Should().Be(TestAddresses.London());
        loaded.Memberships.Should().Equal(customer.Memberships);
        loaded.Version.Should().Be(0);
    }

    [Fact]
    public async Task Each_load_returns_an_independent_instance()
    {
        var scope = NewScope();
        scope.Customers.Add(new CustomerBuilder().WithId(1).Build());
        await scope.CommitAsync();
        var reader = NewScope();

        var first = (await reader.Customers.FindAsync(new CustomerId(1), CancellationToken.None))!;
        var second = (await reader.Customers.FindAsync(new CustomerId(1), CancellationToken.None))!;
        first.ActivateMembership(MembershipType.BookClub, TestClock.Now);

        first.Should().NotBeSameAs(second);
        second.Memberships.Should().BeEmpty();
        (await NewScope().Customers.FindAsync(new CustomerId(1), CancellationToken.None))!.Memberships.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_persists_the_change_and_bumps_the_version()
    {
        var writer = NewScope();
        writer.Customers.Add(new CustomerBuilder().WithId(1).Build());
        await writer.CommitAsync();

        var updater = NewScope();
        var customer = (await updater.Customers.FindAsync(new CustomerId(1), CancellationToken.None))!;
        customer.ActivateMembership(MembershipType.VideoClub, TestClock.Now);
        updater.Customers.Update(customer);
        await updater.CommitAsync();

        var reloaded = (await NewScope().Customers.FindAsync(new CustomerId(1), CancellationToken.None))!;
        reloaded.Version.Should().Be(1);
        reloaded.Memberships.Should().ContainSingle().Which.Type.Should().Be(MembershipType.VideoClub);
    }

    [Fact]
    public async Task Identities_are_generated_in_increasing_order()
    {
        var scope = NewScope();

        var first = await scope.Customers.NextIdentityAsync(CancellationToken.None);
        var second = await scope.Customers.NextIdentityAsync(CancellationToken.None);

        second.Value.Should().BeGreaterThan(first.Value);
    }

    [Fact]
    public async Task Honours_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var scope = NewScope();

        var find = () => scope.Customers.FindAsync(new CustomerId(1), cancellation.Token);
        var next = () => scope.Customers.NextIdentityAsync(cancellation.Token);

        await find.Should().ThrowAsync<OperationCanceledException>();
        await next.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Rejects_null_arguments()
    {
        var scope = NewScope();

        var nullStore = () => new InMemoryCustomerRepository(null!, scope.UnitOfWork);
        var nullUnitOfWork = () => new InMemoryCustomerRepository(_store, null!);
        var nullAdd = () => scope.Customers.Add(null!);
        var nullUpdate = () => scope.Customers.Update(null!);

        nullStore.Should().Throw<ArgumentNullException>();
        nullUnitOfWork.Should().Throw<ArgumentNullException>();
        nullAdd.Should().Throw<ArgumentNullException>();
        nullUpdate.Should().Throw<ArgumentNullException>();
    }
}
