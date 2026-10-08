using FluentAssertions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Tests.Catalog;

public sealed class MembershipPlanTests
{
    [Fact]
    public void Holds_type_name_and_price()
    {
        var plan = new MembershipPlan(MembershipType.Premium, " Premium Membership ", Money.Of(30m));

        plan.Type.Should().Be(MembershipType.Premium);
        plan.Name.Should().Be("Premium Membership");
        plan.Price.Should().Be(Money.Of(30m));
    }

    [Fact]
    public void Rejects_undefined_membership_types()
    {
        var act = () => new MembershipPlan((MembershipType)77, "Mystery", Money.Zero);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("membership_type.unsupported");
    }

    [Fact]
    public void Requires_a_name()
    {
        var act = () => new MembershipPlan(MembershipType.BookClub, " ", Money.Zero);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("membership_plan.name.invalid");
    }
}
