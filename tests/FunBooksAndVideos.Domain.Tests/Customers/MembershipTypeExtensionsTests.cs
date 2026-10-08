using FluentAssertions;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Domain.Tests.Customers;

public sealed class MembershipTypeExtensionsTests
{
    [Theory]
    [InlineData(MembershipType.BookClub, Club.Book)]
    [InlineData(MembershipType.VideoClub, Club.Video)]
    [InlineData(MembershipType.Premium, Club.Book | Club.Video)]
    public void GetClubs_maps_each_membership_to_its_clubs(MembershipType type, Club expected)
    {
        type.GetClubs().Should().Be(expected);
    }

    [Theory]
    [InlineData(MembershipType.BookClub, "Book Club Membership")]
    [InlineData(MembershipType.VideoClub, "Video Club Membership")]
    [InlineData(MembershipType.Premium, "Premium Membership")]
    public void GetDisplayName_matches_the_wording_of_the_kata(MembershipType type, string expected)
    {
        type.GetDisplayName().Should().Be(expected);
    }

    [Theory]
    [InlineData(MembershipType.BookClub, MembershipType.BookClub, true)]
    [InlineData(MembershipType.BookClub, MembershipType.VideoClub, false)]
    [InlineData(MembershipType.BookClub, MembershipType.Premium, true)]
    [InlineData(MembershipType.VideoClub, MembershipType.Premium, true)]
    [InlineData(MembershipType.Premium, MembershipType.Premium, true)]
    [InlineData(MembershipType.VideoClub, MembershipType.BookClub, false)]
    public void OverlapsWith_is_true_when_the_memberships_share_a_club(MembershipType left, MembershipType right, bool expected)
    {
        left.OverlapsWith(right).Should().Be(expected);
        right.OverlapsWith(left).Should().Be(expected);
    }

    [Fact]
    public void Undefined_membership_types_are_rejected()
    {
        var clubs = () => ((MembershipType)0).GetClubs();
        var name = () => ((MembershipType)0).GetDisplayName();

        clubs.Should().Throw<DomainValidationException>().Which.Code.Should().Be("membership_type.unsupported");
        name.Should().Throw<DomainValidationException>().Which.Code.Should().Be("membership_type.unsupported");
    }

    [Fact]
    public void Membership_value_object_rejects_undefined_types_and_exposes_clubs()
    {
        var act = () => new Membership((MembershipType)9, DateTimeOffset.UnixEpoch);
        act.Should().Throw<DomainValidationException>();

        new Membership(MembershipType.Premium, DateTimeOffset.UnixEpoch).Clubs.Should().Be(Club.Book | Club.Video);
    }
}
