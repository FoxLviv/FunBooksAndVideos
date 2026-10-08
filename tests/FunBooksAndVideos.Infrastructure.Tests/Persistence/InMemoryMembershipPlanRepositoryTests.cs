using FluentAssertions;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Mapping;
using FunBooksAndVideos.Infrastructure.Persistence.Repositories;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Infrastructure.Tests.Persistence;

public sealed class InMemoryMembershipPlanRepositoryTests
{
    private readonly InMemoryDataStore _store = new();
    private readonly InMemoryMembershipPlanRepository _repository;

    public InMemoryMembershipPlanRepositoryTests()
    {
        _store.Seed([], [], TestPlans.All().Reverse().Select(MembershipPlanRecordMapper.ToRecord), firstPurchaseOrderId: 1);
        _repository = new InMemoryMembershipPlanRepository(_store);
    }

    [Theory]
    [InlineData(MembershipType.BookClub, 15.00)]
    [InlineData(MembershipType.VideoClub, 20.00)]
    [InlineData(MembershipType.Premium, 30.00)]
    public async Task Find_returns_the_plan_for_a_type(MembershipType type, decimal expectedPrice)
    {
        var plan = await _repository.FindAsync(type, CancellationToken.None);

        plan.Should().NotBeNull();
        plan!.Type.Should().Be(type);
        plan.Price.Amount.Should().Be(expectedPrice);
        plan.Name.Should().Be(type.GetDisplayName());
    }

    [Fact]
    public async Task Find_returns_null_for_unknown_types()
    {
        (await _repository.FindAsync((MembershipType)42, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task List_returns_plans_ordered_by_type()
    {
        var plans = await _repository.ListAsync(CancellationToken.None);

        plans.Select(plan => plan.Type).Should().Equal(MembershipType.BookClub, MembershipType.VideoClub, MembershipType.Premium);
    }

    [Fact]
    public void Rejects_a_null_store()
    {
        var act = () => new InMemoryMembershipPlanRepository(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
