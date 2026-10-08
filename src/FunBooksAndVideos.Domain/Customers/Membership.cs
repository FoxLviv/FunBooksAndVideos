using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.Customers;

/// <summary>An activated membership on a customer account.</summary>
public sealed record Membership
{
    public Membership(MembershipType type, DateTimeOffset activatedAt)
    {
        Type = Guard.DefinedEnum(type, "membership_type.unsupported");
        ActivatedAt = activatedAt;
    }

    public MembershipType Type { get; }

    public DateTimeOffset ActivatedAt { get; }

    public Club Clubs => Type.GetClubs();
}
