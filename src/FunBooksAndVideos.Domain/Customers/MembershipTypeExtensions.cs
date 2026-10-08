using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.Customers;

public static class MembershipTypeExtensions
{
    /// <summary>Clubs the membership grants access to.</summary>
    public static Club GetClubs(this MembershipType type) =>
        type switch
        {
            MembershipType.BookClub => Club.Book,
            MembershipType.VideoClub => Club.Video,
            MembershipType.Premium => Club.Book | Club.Video,
            _ => throw new DomainValidationException("membership_type.unsupported", $"{type} is not a supported membership type."),
        };

    /// <summary>Human readable name as it appears on a purchase order line.</summary>
    public static string GetDisplayName(this MembershipType type) =>
        type switch
        {
            MembershipType.BookClub => "Book Club Membership",
            MembershipType.VideoClub => "Video Club Membership",
            MembershipType.Premium => "Premium Membership",
            _ => throw new DomainValidationException("membership_type.unsupported", $"{type} is not a supported membership type."),
        };

    /// <summary><see langword="true"/> when both memberships grant access to at least one common club.</summary>
    public static bool OverlapsWith(this MembershipType type, MembershipType other) =>
        (type.GetClubs() & other.GetClubs()) != Club.None;
}
