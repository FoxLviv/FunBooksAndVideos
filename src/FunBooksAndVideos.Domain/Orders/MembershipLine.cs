using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Orders;

/// <summary>A membership request, priced at the plan's price at purchase time.</summary>
public sealed class MembershipLine : OrderLine
{
    public MembershipLine(MembershipType membershipType, Money price)
        : base(price)
    {
        MembershipType = Guard.DefinedEnum(membershipType, "membership_type.unsupported");
    }

    public MembershipType MembershipType { get; }

    public override string Description => MembershipType.GetDisplayName();

    public static MembershipLine For(MembershipPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new MembershipLine(plan.Type, plan.Price);
    }

    public override TResult Accept<TResult>(IOrderLineVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.VisitMembership(this);
    }
}
