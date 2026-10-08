using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Catalog;

/// <summary>Sellable membership: the price the shop charges for a given <see cref="MembershipType"/>.</summary>
public sealed class MembershipPlan
{
    public const int MaxNameLength = 200;

    public MembershipPlan(MembershipType type, string name, Money price)
    {
        Type = Guard.DefinedEnum(type, "membership_type.unsupported");
        Name = Guard.RequiredText(name, MaxNameLength, "membership_plan.name.invalid", "Membership plan name");
        Price = price;
    }

    public MembershipType Type { get; }

    public string Name { get; }

    public Money Price { get; }
}
